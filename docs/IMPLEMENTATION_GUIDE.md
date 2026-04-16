# Hướng dẫn triển khai Feature mới — PRN232 Auto Grading Tool

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Coding Convention](#2-coding-convention)
3. [Cấu trúc thư mục cho một Feature](#3-cấu-trúc-thư-mục-cho-một-feature)
4. [EntityExtension — Ghi audit field & khởi tạo ID](#4-entityextension--ghi-audit-field--khởi-tạo-id)
5. [Generic Repository & Unit of Work](#5-generic-repository--unit-of-work)
6. [QueryBuilder — Xây predicate & sắp xếp](#6-querybuilder--xây-predicate--sắp-xếp)
7. [CRUD API — Step-by-step](#7-crud-api--step-by-step)
8. [Trả về HTTP status code đúng cách](#8-trả-về-http-status-code-đúng-cách)
9. [Background Job với Hangfire](#9-background-job-với-hangfire)
10. [Đăng ký DI](#10-đăng-ký-di)
11. [Checklist triển khai feature mới](#11-checklist-triển-khai-feature-mới)

---

## 1. Tổng quan kiến trúc

Dự án sử dụng **Clean Architecture** kết hợp **CQRS** qua MediatR:

```
API         → nhận HTTP request, gọi Mediator.Send()
Application → định nghĩa Commands/Queries/Handlers/Validators
Domain      → Entities, Enums, Domain interfaces
Infrastructure → DbContext, Repositories, Services, Hangfire Jobs
```

Luồng xử lý một request:

```
HTTP Request
    → Controller.Action()
    → _mediator.Send(new XyzCommand(...))
    → ValidationBehavior (FluentValidation tự động chạy trước Handler)
    → XyzCommandHandler.Handle()
        → IsUserValidAsync() / ValidateUserWithRolesAsync()  ← kiểm tra auth
        → Repository<Entity>...            ← đọc/ghi DB
        → entity.InitializeEntity(userId)  ← ghi audit fields (Create)
        → entity.UpdateEntity(userId)      ← ghi audit fields (Update)
        → SaveChangesAsync()
    → Result<T> / Result
    → StatusCode(result.GetHttpStatusCode(), result)
    → HTTP Response
```

---

## 2. Coding Convention

### Namespace

| Layer          | Namespace prefix                                   |
|----------------|----------------------------------------------------|
| API            | `PRN232_G9_AutoGradingTool.API`                    |
| Application    | `PRN232_G9_AutoGradingTool.Application`            |
| Domain         | `PRN232_G9_AutoGradingTool.Domain`                 |
| Infrastructure | `PRN232_G9_AutoGradingTool.Infrastructure`         |

### Đặt tên file

| Thành phần      | Pattern                                      | Ví dụ                            |
|-----------------|----------------------------------------------|----------------------------------|
| Command         | `{Action}{Entity}Command.cs`                 | `CreateExamCommand.cs`           |
| CommandHandler  | `{Action}{Entity}CommandHandler.cs`          | `CreateExamCommandHandler.cs`    |
| Validator       | `{Action}{Entity}CommandValidator.cs`        | `CreateExamCommandValidator.cs`  |
| Query           | `Get{Entity}(s)Query.cs`                     | `GetExamsQuery.cs`               |
| QueryHandler    | `Get{Entity}(s)QueryHandler.cs`              | `GetExamsQueryHandler.cs`        |
| DTO             | `{Entity}Request.cs` / `{Entity}Response.cs` | `ExamRequest.cs`                 |
| Job             | `{Action}Job.cs`                             | `GradeSubmissionJob.cs`          |

### Result & Exception

- Trả về `Result<T>` (có data) hoặc `Result` (không data) — **không** trả `null` hay throw exception tuỳ tiện.
- Dùng exception cho lỗi nghiệp vụ:
  - `NotFoundException` — entity không tồn tại
  - `BusinessRuleViolationException` — vi phạm rule nghiệp vụ
  - `UnauthorizedAccessException` — chưa đăng nhập / hết session
- Exception sẽ được middleware xử lý và map sang HTTP status tương ứng.

### Xác thực người dùng trong Handler

`ICurrentUserService` cung cấp 3 method tuỳ mức độ cần thiết:

```csharp
// Chỉ cần biết userId (không cần role, không cần entity)
var (isValid, userId) = await _currentUserService.IsUserValidAsync();
if (!isValid || userId == null) throw new UnauthorizedAccessException();

// Cần role để kiểm tra quyền
var (isValid, userId, roles) = await _currentUserService.ValidateUserWithRolesAsync();
if (!isValid || userId == null) throw new UnauthorizedAccessException();

// Cần cả user entity (ít dùng hơn)
var (isValid, userId, roles, user) = await _currentUserService.ValidateUserWithRolesAndEntityAsync();
if (!isValid || userId == null) throw new UnauthorizedAccessException();
```

Các method này đều cache kết quả trong cùng một HTTP request — không lo query DB nhiều lần.

`RoleEnum` của dự án:

```csharp
public enum RoleEnum
{
    SystemAdmin,   // quản trị hệ thống
    Instructor     // giảng viên
}
```

---

## 3. Cấu trúc thư mục cho một Feature

```
Application/
└── Features/
    └── Exams/                          ← tên feature (số nhiều)
        ├── Commands/
        │   ├── CreateExam/
        │   │   ├── CreateExamCommand.cs
        │   │   ├── CreateExamCommandHandler.cs
        │   │   └── CreateExamCommandValidator.cs
        │   ├── UpdateExam/
        │   │   ├── UpdateExamCommand.cs
        │   │   ├── UpdateExamCommandHandler.cs
        │   │   └── UpdateExamCommandValidator.cs
        │   └── DeleteExam/
        │       ├── DeleteExamCommand.cs
        │       └── DeleteExamCommandHandler.cs
        └── Queries/
            ├── GetExams/
            │   ├── GetExamsQuery.cs
            │   └── GetExamsQueryHandler.cs
            └── GetExamById/
                ├── GetExamByIdQuery.cs
                └── GetExamByIdQueryHandler.cs
```

---

## 4. EntityExtension — Ghi audit field & khởi tạo ID

Mọi entity trong dự án đều implement `IEntityLike` (có `Id`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`). Thay vì gán thủ công từng field, **luôn dùng extension method** sau:

### InitializeEntity — dùng khi CREATE

Gọi **sau khi map từ request** và **trước khi AddAsync**:

```csharp
var exam = _mapper.Map<Exam>(command.Request);

// Tự động: gán Id mới (nếu chưa có), set CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
exam.InitializeEntity(userId);

await _unitOfWork.Repository<Exam>().AddAsync(exam, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### UpdateEntity — dùng khi UPDATE

Gọi **sau khi map request vào entity** và **trước khi Update**:

```csharp
// Giữ lại CreatedAt, CreatedBy từ entity cũ; chỉ cập nhật UpdatedAt, UpdatedBy
_mapper.Map(command.Request, exam);
exam.UpdateEntity(userId);

_unitOfWork.Repository<Exam>().Update(exam);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### SoftDeleteEntity — dùng khi SOFT DELETE

Chỉ dùng cho entity kế thừa `BaseEntity` (có `IsDeleted`, `DeletedAt`, `DeletedBy`):

```csharp
exam.SoftDeleteEntity(userId);
// Không cần Delete() — chỉ cần SaveChangesAsync để ghi flag IsDeleted = true
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

> **Global Query Filter**: `BaseDbContext` đã cấu hình `HasQueryFilter(e => e.IsDeleted == false)` cho mọi entity kế thừa `BaseEntity`. Mọi query qua Repository sẽ tự động bỏ qua bản ghi đã soft delete — **không cần viết `predicate: e => e.IsDeleted == false` thủ công**.  
> Nếu cần truy vấn cả bản ghi đã xóa, dùng `IgnoreQueryFilters()` trực tiếp trên `IQueryable`.

---

## 5. Generic Repository & Unit of Work

### IUnitOfWork — các method quan trọng

```csharp
// Lấy repository cho bất kỳ entity nào
IGenericRepository<T> Repository<T>() where T : class, IEntityLike;

// Lưu thay đổi
Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

// Quản lý transaction thủ công (dùng khi cần atomic nhiều bước)
Task BeginTransactionAsync(CancellationToken cancellationToken = default);
Task CommitTransactionAsync(CancellationToken cancellationToken = default);
Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

// Chạy raw SQL (không expose DbContext ra ngoài)
Task<int> ExecuteSqlRawAsync(string sql, object[]? parameters = null, ...);
```

### IGenericRepository\<T\> — các method thường dùng

```csharp
// Thêm
Task AddAsync(T entity, CancellationToken ct = default);
Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

// Lấy
Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
    Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default);

// Danh sách + phân trang
Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
    int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate,
    Expression<Func<T, object>>? orderBy, bool isAscending, ...);

// Kiểm tra tồn tại
Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

// Xoá / Update (void — gọi SaveChangesAsync sau)
void Delete(T entity);
void Update(T entity);

// Bulk operation (không load entity vào memory)
Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
Task<int> ExecuteUpdateAsync(
    Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties,
    Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

IQueryable<T> GetQueryable();
```

### Ví dụ sử dụng cơ bản

```csharp
// CREATE
var exam = _mapper.Map<Exam>(request);
await _unitOfWork.Repository<Exam>().AddAsync(exam, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// READ (single)
var exam = await _unitOfWork.Repository<Exam>()
    .GetFirstOrDefaultAsync(e => e.Id == id, cancellationToken: cancellationToken);
if (exam == null) throw new NotFoundException(...);

// READ (paged list)
// ⚠️ Không cần predicate IsDeleted — global query filter trong BaseDbContext đã tự lọc
var (items, total) = await _unitOfWork.Repository<Exam>()
    .GetPagedAsync(pageNumber: page, pageSize: pageSize,
        predicate: null,  // hoặc điều kiện lọc khác nếu cần, VD: e => e.Status == EntityStatusEnum.Active
        orderBy: e => e.CreatedAt, isAscending: false,
        queryCustomizer: null, includes: null,
        cancellationToken: cancellationToken);

// UPDATE (load → map → UpdateEntity → Update → save)
var exam = await _unitOfWork.Repository<Exam>()
    .GetFirstOrDefaultAsync(e => e.Id == id, cancellationToken: cancellationToken);
if (exam == null) throw new NotFoundException(...);
_mapper.Map(request, exam);
exam.UpdateEntity(userId);
_unitOfWork.Repository<Exam>().Update(exam);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// DELETE (soft delete)
exam.SoftDeleteEntity(userId);
_unitOfWork.Repository<Exam>().Update(exam);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// DELETE (hard delete)
_unitOfWork.Repository<Exam>().Delete(exam);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// BULK UPDATE không load entity
await _unitOfWork.Repository<Submission>()
    .ExecuteUpdateAsync(
        s => s.SetProperty(x => x.Status, SubmissionStatus.Expired),
        predicate: x => x.ExamId == examId && x.Status == SubmissionStatus.Pending,
        cancellationToken: cancellationToken);
```

### Dùng Transaction khi cần atomic

```csharp
await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    await _unitOfWork.Repository<Exam>().AddAsync(exam, cancellationToken);
    await _unitOfWork.Repository<ExamQuestion>().AddRangeAsync(questions, cancellationToken);
    // CommitTransactionAsync đã tự gọi SaveChangesAsync bên trong — không gọi thêm
    await _unitOfWork.CommitTransactionAsync(cancellationToken);
}
catch
{
    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

---

## 6. QueryBuilder — Xây predicate & sắp xếp

Khi danh sách có nhiều điều kiện lọc / sắp xếp, **không viết predicate thủ công trong handler**. Dùng `GenericQueryBuilder<TEntity, TFilter>` — đã có sẵn trong `Application/Common/QueryBuilders/`.

### Tại sao cần QueryBuilder?

Handler chỉ nên lo một việc: điều phối luồng nghiệp vụ. Việc dịch `ExamFilter` sang `Expression<Func<Exam, bool>>` nếu viết thẳng trong handler sẽ:
- Làm handler dài, khó đọc.
- Logic lọc bị rải rác ở nhiều chỗ, khó tái sử dụng.
- Không dễ test độc lập.

QueryBuilder giải quyết bằng cách **khai báo rule một lần, dùng nhiều nơi**.

### Tạo QueryBuilder cho một Feature

```csharp
// Application/Common/QueryBuilders/ExamQueryBuilder.cs
using System.Linq.Expressions;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Exam;
using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Application.Common.QueryBuilders;

public static class ExamQueryBuilder
{
    private static GenericQueryBuilder<Exam, ExamFilter> CreateBuilder(ExamFilter filter)
    {
        return filter.CreateQueryBuilder<Exam, ExamFilter>()
            // Search: tìm theo Title (contains, case-insensitive)
            .AddSearchProperty(x => (object)x.Title)
            .AddSearchProperty(x => (object)(x.Description ?? string.Empty))

            // Filter: theo trạng thái
            .AddFilterRule(
                f => f.Status.HasValue,
                f => x => x.Status == f.Status!.Value)

            // Filter: theo người tạo
            .AddFilterRule(
                f => f.CreatedBy.HasValue,
                f => x => x.CreatedBy == f.CreatedBy!.Value)

            // Filter: khoảng thời gian tạo
            .AddFilterRule(
                f => f.CreatedFrom.HasValue,
                f => x => x.CreatedAt >= f.CreatedFrom!.Value)
            .AddFilterRule(
                f => f.CreatedTo.HasValue,
                f => x => x.CreatedAt <= f.CreatedTo!.Value.AddDays(1))

            // Sort mappings: client truyền sortBy = "title" | "createdat"
            .AddSortMapping("title",     x => x.Title)
            .AddSortMapping("createdat", x => x.CreatedAt!)
            .AddSortMapping("startsat",  x => x.StartsAt!)

            // Default: mới nhất trước
            .SetDefaultOrderBy(x => x.CreatedAt!);
    }

    // Hai extension method này được gọi trực tiếp từ Handler
    public static Expression<Func<Exam, bool>> BuildPredicate(this ExamFilter filter)
        => CreateBuilder(filter).BuildPredicate();

    public static Expression<Func<Exam, object>> BuildOrderBy(this ExamFilter filter)
        => CreateBuilder(filter).BuildOrderBy();
}
```

### Dùng trong Handler

```csharp
// GetExamsQueryHandler.cs — gọn hơn hẳn so với viết predicate thủ công
var predicate  = filter.BuildPredicate();
var orderBy    = filter.BuildOrderBy();
var isAscending = filter.IsAscending ?? false;

var (exams, total) = await _unitOfWork.Repository<Exam>().GetPagedAsync(
    pageNumber:    page,
    pageSize:      pageSize,
    predicate:     predicate,
    orderBy:       orderBy,
    isAscending:   isAscending,
    queryCustomizer: null,
    includes:      null,
    cancellationToken: cancellationToken);
```

### ExamFilter DTO

```csharp
// Application/Common/DTOs/Exam/ExamFilter.cs
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

public class ExamFilter : BasePaginationFilter
{
    // BasePaginationFilter đã có: Page, PageSize, Search, SortBy, IsAscending
    
    public ExamStatusEnum? Status { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}
```

> Client gọi: `GET /api/cms/exams?search=Math&status=Active&sortBy=createdat&isAscending=false&page=1&pageSize=20`

---

## 7. CRUD API — Step-by-step

Dưới đây là ví dụ đầy đủ cho entity **`Exam`**, theo đúng pattern.

### Bước 1 — DTO

```csharp
// Application/Common/DTOs/Exam/ExamRequest.cs
public class ExamRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
}

// Application/Common/DTOs/Exam/ExamListItem.cs
public class ExamListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Application/Common/DTOs/Exam/ExamDetailResponse.cs  
public class ExamDetailResponse : ExamListItem
{
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Bước 2 — Commands

#### Create

```csharp
// Application/Features/Exams/Commands/CreateExam/CreateExamCommand.cs
public record CreateExamCommand(ExamRequest Request) : IRequest<Result>;
```

```csharp
// Application/Features/Exams/Commands/CreateExam/CreateExamCommandHandler.cs
public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<CreateExamCommandHandler> _logger;

    // constructor...

    public async Task<Result> Handle(CreateExamCommand command, CancellationToken cancellationToken)
    {
        // 1. Xác thực người dùng
        var (isValid, userId) = await _currentUserService.IsUserValidAsync();
        if (!isValid || userId == null)
            throw new UnauthorizedAccessException();

        // 2. Kiểm tra duplicate
        var isDuplicate = await _unitOfWork.Repository<Exam>()
            .AnyAsync(e => e.Title == command.Request.Title.Trim(), cancellationToken);
        if (isDuplicate)
            throw new BusinessRuleViolationException("Tên kỳ thi đã tồn tại.");

        // 3. Map → khởi tạo audit field → lưu
        var exam = _mapper.Map<Exam>(command.Request);
        exam.InitializeEntity(userId);   // ← tự động gán Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy

        await _unitOfWork.Repository<Exam>().AddAsync(exam, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create exam");
            throw;
        }

        _logger.LogInformation("Exam created: {ExamId} by user {UserId}", exam.Id, userId);
        return Result.Success("Tạo kỳ thi thành công.");
    }
}
```

```csharp
// Application/Features/Exams/Commands/CreateExam/CreateExamCommandValidator.cs
public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator()
    {
        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Tiêu đề không được trống.")
            .MaximumLength(200);

        RuleFor(x => x.Request.DurationMinutes)
            .GreaterThan(0).WithMessage("Thời gian thi phải lớn hơn 0 phút.");

        RuleFor(x => x.Request.StartsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Thời gian bắt đầu phải trong tương lai.");
    }
}
```

#### Update

```csharp
// Application/Features/Exams/Commands/UpdateExam/UpdateExamCommand.cs
public record UpdateExamCommand(Guid Id, ExamRequest Request) : IRequest<Result>;
```

```csharp
// Handler
public async Task<Result> Handle(UpdateExamCommand command, CancellationToken cancellationToken)
{
    var (isValid, userId) = await _currentUserService.IsUserValidAsync();
    if (!isValid || userId == null) throw new UnauthorizedAccessException();

    var exam = await _unitOfWork.Repository<Exam>()
        .GetFirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken: cancellationToken);

    if (exam == null)
        throw new NotFoundException("Kỳ thi không tồn tại.", typeof(Exam));

    var oldExam = exam;  // giữ reference để UpdateEntity bảo toàn CreatedAt, CreatedBy

    _mapper.Map(command.Request, exam);
    exam.UpdateEntity(userId, oldExam);  // ← giữ CreatedAt/CreatedBy từ oldExam, cập nhật UpdatedAt/UpdatedBy

    _unitOfWork.Repository<Exam>().Update(exam);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return Result.Success("Cập nhật thành công.");
}
```

#### Delete

```csharp
// Application/Features/Exams/Commands/DeleteExam/DeleteExamCommand.cs
public record DeleteExamCommand(Guid Id) : IRequest<Result>;
```

```csharp
// Handler
public async Task<Result> Handle(DeleteExamCommand command, CancellationToken cancellationToken)
{
    var (isValid, userId) = await _currentUserService.IsUserValidAsync();
    if (!isValid || userId == null) throw new UnauthorizedAccessException();

    var exam = await _unitOfWork.Repository<Exam>()
        .GetFirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken: cancellationToken);

    if (exam == null)
        throw new NotFoundException("Kỳ thi không tồn tại.", typeof(Exam));

    // Kiểm tra ràng buộc — dùng AnyAsync, không cần load toàn bộ collection
    var hasSubmissions = await _unitOfWork.Repository<Submission>()
        .AnyAsync(s => s.ExamId == command.Id, cancellationToken);
    if (hasSubmissions)
        throw new BusinessRuleViolationException("Không thể xóa kỳ thi đã có bài nộp.");

    _unitOfWork.Repository<Exam>().Delete(exam);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return Result.Success("Xóa thành công.");
}
```

### Bước 3 — Queries

```csharp
// Application/Features/Exams/Queries/GetExams/GetExamsQuery.cs
public record GetExamsQuery(ExamFilter Filter) : IRequest<PaginationResult<ExamListItem>>;
```

```csharp
// Handler — sử dụng QueryBuilder (xem mục 6)
public async Task<PaginationResult<ExamListItem>> Handle(GetExamsQuery request, CancellationToken cancellationToken)
{
    var (isValid, userId) = await _currentUserService.IsUserValidAsync();
    if (!isValid || userId == null) throw new UnauthorizedAccessException();

    var filter = request.Filter;
    var page = filter.Page > 0 ? filter.Page : 1;
    var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

    var predicate  = filter.BuildPredicate();
    var orderBy    = filter.BuildOrderBy();
    var isAscending = filter.IsAscending ?? false;

    var (exams, total) = await _unitOfWork.Repository<Exam>()
        .GetPagedAsync(page, pageSize, predicate, orderBy, isAscending,
            queryCustomizer: null, includes: null,
            cancellationToken: cancellationToken);

    var items = _mapper.Map<List<ExamListItem>>(exams);
    return PaginationResult<ExamListItem>.Success(items, page, pageSize, total, "Lấy danh sách thành công.");
}
```

```csharp
// Application/Features/Exams/Queries/GetExamById/GetExamByIdQuery.cs
public record GetExamByIdQuery(Guid Id) : IRequest<Result<ExamDetailResponse>>;
```

```csharp
// Handler
public async Task<Result<ExamDetailResponse>> Handle(GetExamByIdQuery query, CancellationToken cancellationToken)
{
    var (isValid, userId) = await _currentUserService.IsUserValidAsync();
    if (!isValid || userId == null) throw new UnauthorizedAccessException();

    var exam = await _unitOfWork.Repository<Exam>()
        .GetFirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken: cancellationToken);

    if (exam == null)
        throw new NotFoundException("Kỳ thi không tồn tại.", typeof(Exam));

    var dto = _mapper.Map<ExamDetailResponse>(exam);
    return Result<ExamDetailResponse>.Success(dto, "Lấy thông tin thành công.");
}
```

### Bước 4 — Controller

```csharp
// API/Controllers/Cms/ExamsController.cs
[ApiController]
[Route("api/cms/exams")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("Exam management")]
public class ExamsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExamsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AuthorizeRoles(nameof(RoleEnum.Instructor), nameof(RoleEnum.SystemAdmin))]
    [ProducesResponseType(typeof(PaginationResult<ExamListItem>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get exams (paged)", OperationId = "GetExams")]
    public async Task<IActionResult> GetExams([FromQuery] ExamFilter filter)
    {
        var result = await _mediator.Send(new GetExamsQuery(filter));
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    [HttpGet("{id:guid}")]
    [AuthorizeRoles(nameof(RoleEnum.Instructor), nameof(RoleEnum.SystemAdmin))]
    [ProducesResponseType(typeof(Result<ExamDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExamDetailResponse>), StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get exam by ID", OperationId = "GetExamById")]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        var result = await _mediator.Send(new GetExamByIdQuery(id));
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    [HttpPost]
    [AuthorizeRoles(nameof(RoleEnum.Instructor), nameof(RoleEnum.SystemAdmin))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Create new exam", OperationId = "CreateExam")]
    public async Task<IActionResult> CreateExam([FromBody] ExamRequest request)
    {
        var result = await _mediator.Send(new CreateExamCommand(request));
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    [HttpPatch("{id:guid}")]
    [AuthorizeRoles(nameof(RoleEnum.Instructor), nameof(RoleEnum.SystemAdmin))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update exam", OperationId = "UpdateExam")]
    public async Task<IActionResult> UpdateExam(Guid id, [FromBody] ExamRequest request)
    {
        var result = await _mediator.Send(new UpdateExamCommand(id, request));
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    [HttpDelete("{id:guid}")]
    [AuthorizeRoles(nameof(RoleEnum.SystemAdmin))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Delete exam", OperationId = "DeleteExam")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        var result = await _mediator.Send(new DeleteExamCommand(id));
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}
```

---

## 8. Trả về HTTP status code đúng cách

Mọi action method trong Controller đều dùng **một dòng duy nhất**:

```csharp
return StatusCode(result.GetHttpStatusCode(), result);
```

### Cơ chế hoạt động

`GetHttpStatusCode()` là extension method định nghĩa trong `Application/Common/Extensions/ResultExtensions.cs`:

```csharp
public static int GetHttpStatusCode<T>(this Result<T> result)
{
    if (result.IsSuccess) return StatusCodes.Status200OK;
    if (!Enum.TryParse<ErrorCodeEnum>(result.ErrorCode, out var errorCode))
        return StatusCodes.Status500InternalServerError;
    return errorCode.ToHttpStatusCode();
}
```

`ToHttpStatusCode()` là switch-case trong `ErrorCodeEnumExtensions.cs` map `ErrorCodeEnum` sang HTTP status:

| ErrorCodeEnum              | HTTP Status |
|----------------------------|-------------|
| `ValidationFailed`         | 400         |
| `DuplicateEntry`           | 400         |
| `InvalidInput`             | 400         |
| `BusinessRuleViolation`    | 422         |
| `ResourceConflict`         | 422         |
| `Unauthorized`             | 401         |
| `TokenExpired`             | 401         |
| `Forbidden`                | 403         |
| `NotFound`                 | 404         |
| `TooManyRequests`          | 429         |
| `InternalError`            | 500         |
| `DatabaseError`            | 500         |
| `ExternalServiceError`     | 500         |

### Tại sao không hardcode status trong Controller?

Khi handler throw exception, middleware sẽ map sang `Result` với `ErrorCode` phù hợp. Controller không cần biết lỗi là gì — chỉ cần gọi `GetHttpStatusCode()` và status sẽ đúng tự động.

```csharp
// ✅ Đúng — status được tự động map từ ErrorCode bên trong Result
return StatusCode(result.GetHttpStatusCode(), result);
```

---

## 9. Background Job với Hangfire

Dự án đã tích hợp sẵn Hangfire (bật qua `Hangfire:UseOuterDatabase = true` trong config). Dùng pattern sau để offload tác vụ nặng/chậm ra background.

### 9.1 — Tạo Job class

Job class đặt trong `Infrastructure/Jobs/`. Inject `IServiceProvider` để tự tạo scope khi chạy (tránh DbContext lifetime issue).

```csharp
// Infrastructure/Jobs/GradeSubmissionJob.cs
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

/// <summary>
/// Hangfire job tự động chấm điểm submission sau khi nộp bài.
/// Queue: "grading" — tách biệt với queue mặc định để không block các job khác.
/// </summary>
public class GradeSubmissionJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GradeSubmissionJob> _logger;

    public GradeSubmissionJob(IServiceProvider serviceProvider, ILogger<GradeSubmissionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [Queue("grading")]
    public async Task ExecuteAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // ⚠️ QUAN TRỌNG: Luôn tạo scope mới — không dùng DI trực tiếp từ constructor
            await using var scope = _serviceProvider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var gradingService = scope.ServiceProvider.GetRequiredService<IExamGradingAppService>();

            var submission = await unitOfWork.Repository<Submission>()
                .GetFirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken: cancellationToken);

            if (submission == null)
            {
                _logger.LogWarning("GradeSubmissionJob: Submission {Id} not found.", submissionId);
                return;
            }

            await gradingService.GradeAsync(submission, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Graded submission {Id} successfully.", submissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GradeSubmissionJob failed for submission {Id}", submissionId);
            throw; // ⚠️ throw để Hangfire retry tự động
        }
    }
}
```

### 9.2 — Tạo interface cho service gọi job

```csharp
// Application/Common/Interfaces/IBackgroundGradingService.cs
namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

public interface IBackgroundGradingService
{
    /// <summary>
    /// Enqueue chấm điểm bất đồng bộ sau khi submission được lưu thành công.
    /// </summary>
    void EnqueueGradeSubmission(Guid submissionId);

    /// <summary>
    /// Lên lịch tổng kết kết quả thi sau khi kỳ thi kết thúc.
    /// </summary>
    void ScheduleExamResultSummary(Guid examId, DateTime examEndsAt);
}
```

### 9.3 — Implement service

```csharp
// Infrastructure/Services/BackgroundGradingService.cs
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public sealed class BackgroundGradingService : IBackgroundGradingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundGradingService> _logger;

    public BackgroundGradingService(IServiceScopeFactory scopeFactory, ILogger<BackgroundGradingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void EnqueueGradeSubmission(Guid submissionId)
    {
        // 1. Thử enqueue qua Hangfire (ưu tiên — có retry, dashboard)
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<IBackgroundJobClient>();
        if (client != null)
        {
            try
            {
                client.Enqueue<GradeSubmissionJob>(x => x.ExecuteAsync(submissionId, CancellationToken.None));
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Grading] Hangfire enqueue failed for submission {Id}. Falling back to Task.Run.", submissionId);
            }
        }

        // 2. Fallback: Task.Run nếu Hangfire không khả dụng
        _ = Task.Run(async () =>
        {
            try
            {
                using var fallbackScope = _scopeFactory.CreateScope();
                var job = fallbackScope.ServiceProvider.GetRequiredService<GradeSubmissionJob>();
                await job.ExecuteAsync(submissionId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Grading] Task fallback failed for submission {Id}.", submissionId);
            }
        });
    }

    public void ScheduleExamResultSummary(Guid examId, DateTime examEndsAt)
    {
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<IBackgroundJobClient>();
        if (client == null) return;

        // Lên lịch chạy vào đúng thời điểm kỳ thi kết thúc
        var delay = examEndsAt - DateTime.UtcNow;
        if (delay > TimeSpan.Zero)
        {
            client.Schedule<SummarizeExamResultJob>(
                x => x.ExecuteAsync(examId, CancellationToken.None),
                delay);
        }
    }
}
```

### 9.4 — Gọi job từ Handler

```csharp
// Inject IBackgroundGradingService vào handler thay vì gọi job trực tiếp
public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundGradingService _gradingService;

    public SubmitExamCommandHandler(IUnitOfWork unitOfWork, IBackgroundGradingService gradingService)
    {
        _unitOfWork = unitOfWork;
        _gradingService = gradingService;
    }

    public async Task<Result> Handle(SubmitExamCommand command, CancellationToken cancellationToken)
    {
        // ... lưu submission ...
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ⚠️ Gọi sau khi SaveChanges thành công — đảm bảo data đã được commit
        _gradingService.EnqueueGradeSubmission(submission.Id);

        return Result.Success("Nộp bài thành công. Hệ thống đang chấm điểm.");
    }
}
```

### 9.5 — Cấu hình Queue trong `.env`

Dự án dùng Docker — **không sửa `appsettings.json`**, mọi config Hangfire quản lý trong file `.env`:

```env
Hangfire__UseOuterDatabase=true
Hangfire__WorkerCount=2

# Danh sách queue (mặc định chỉ có notification-system)
# Thêm queue mới vào đây theo dấu phẩy
Hangfire__Queues=notification-system,grading,file-ops

Hangfire__Retry__Attempts=3
Hangfire__Retry__DelayInSeconds__First=60
Hangfire__Retry__DelayInSeconds__Second=300
Hangfire__Retry__DelayInSeconds__Third=600

Hangfire__Limits__MaxConcurrentJobsPerQueue=2
Hangfire__Limits__QueueTimeout=1800
```

> Khi thêm queue mới (ví dụ `grading`), chỉ cần append vào `Hangfire__Queues` — ứng dụng đọc giá trị này khi khởi động để đăng ký worker. Không cần rebuild image, chỉ cần `docker compose up -d --no-build` để restart service.

---

## 10. Đăng ký DI

Sau khi tạo job và service, đăng ký trong `InfrastructureDependencyInjection.cs`:

```csharp
// Đăng ký background service
services.AddScoped<IBackgroundGradingService, BackgroundGradingService>();

// Đăng ký job class (Transient — Hangfire tạo instance mỗi lần chạy)
services.AddTransient<GradeSubmissionJob>();
services.AddTransient<SummarizeExamResultJob>();
```

---

## 11. Checklist triển khai feature mới

### Application layer

- [ ] `Features/{FeatureName}/Commands/{ActionName}/` — Command, Handler, Validator
- [ ] `Features/{FeatureName}/Queries/Get{Entity}(s)/` — Query, Handler
- [ ] DTO trong `Common/DTOs/{FeatureName}/`
- [ ] QueryBuilder trong `Common/QueryBuilders/{Entity}QueryBuilder.cs`
- [ ] AutoMapper profile trong `Common/Mappings/`

### Infrastructure layer

- [ ] (Nếu cần) `Jobs/{Action}Job.cs`
- [ ] (Nếu cần) `Services/Background{Feature}Service.cs`
- [ ] Đăng ký trong `InfrastructureDependencyInjection.cs`

### API layer

- [ ] `Controllers/Cms/{Feature}Controller.cs`
- [ ] Route: `api/cms/{feature-plural}` (kebab-case)
- [ ] Mỗi action dùng `return StatusCode(result.GetHttpStatusCode(), result);`
- [ ] `[AuthorizeRoles(...)]`, `[ProducesResponseType]`, `[SwaggerOperation]`

### Kiểm tra

- [ ] Build thành công
- [ ] Swagger hiển thị đúng endpoint
- [ ] Handler gọi `InitializeEntity` cho CREATE, `UpdateEntity` cho UPDATE
- [ ] `SaveChangesAsync` trong try/catch
- [ ] Background job gọi **sau** `SaveChangesAsync`, không trước

---

## Lưu ý quan trọng

> **Transaction và SaveChanges**
>
> `CommitTransactionAsync` đã tự gọi `SaveChangesAsync` bên trong. Không gọi `SaveChangesAsync` thêm một lần nữa khi dùng manual transaction.

> **Hangfire và DbContext scope**
>
> Không inject `DbContext` hay `IUnitOfWork` vào constructor của Job — luôn tạo scope mới bên trong `ExecuteAsync` bằng `_serviceProvider.CreateAsyncScope()`.

> **Throw để Hangfire retry**
>
> Trong Job, luôn `throw` lại exception — Hangfire dựa vào exception để biết job thất bại và schedule retry.
