using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamGrading;

/// <summary>
/// Cụm Semesters được refactor theo style guide:
/// - AuthorizationBehavior qua [Authorize]
/// - xử lý ở Handler dùng UnitOfWork + EntityExtension
/// </summary>
[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgListSemestersQuery : IRequest<Result<List<SemesterListItemDto>>>;
public sealed class EgListSemestersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListSemestersQuery, Result<List<SemesterListItemDto>>>
{
    public async Task<Result<List<SemesterListItemDto>>> Handle(EgListSemestersQuery request, CancellationToken cancellationToken)
    {
        var rows = await unitOfWork.Repository<Semester>()
            .GetQueryable()
            .OrderBy(x => x.Code)
            .Select(x => new SemesterListItemDto(x.Id, x.Code, x.Name, x.StartDateUtc, x.EndDateUtc))
            .ToListAsync(cancellationToken);
        return Result<List<SemesterListItemDto>>.Success(rows, "OK");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateSemesterCommand(CreateSemesterRequest Body) : IRequest<Result<SemesterListItemDto>>;
public sealed class EgCreateSemesterCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateSemesterCommandHandler> logger)
    : IRequestHandler<EgCreateSemesterCommand, Result<SemesterListItemDto>>
{
    public async Task<Result<SemesterListItemDto>> Handle(EgCreateSemesterCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var code = request.Body.Code.Trim();
        var name = request.Body.Name.Trim();

        var repo = unitOfWork.Repository<Semester>();
        if (await repo.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<SemesterListItemDto>.Failure("Mã học kỳ đã tồn tại.", ErrorCodeEnum.DuplicateEntry);

        var entity = new Semester
        {
            Code = code,
            Name = name,
            StartDateUtc = request.Body.StartDateUtc,
            EndDateUtc = request.Body.EndDateUtc,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);

        await repo.AddAsync(entity, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create semester {Code}", code);
            throw;
        }

        return Result<SemesterListItemDto>.Success(
            new SemesterListItemDto(entity.Id, entity.Code, entity.Name, entity.StartDateUtc, entity.EndDateUtc),
            "Đã tạo học kỳ.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateSemesterCommand(Guid Id, UpdateSemesterRequest Body) : IRequest<Result<SemesterListItemDto>>;
public sealed class EgUpdateSemesterCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateSemesterCommandHandler> logger)
    : IRequestHandler<EgUpdateSemesterCommand, Result<SemesterListItemDto>>
{
    public async Task<Result<SemesterListItemDto>> Handle(EgUpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<Semester>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<SemesterListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = request.Body.Code.Trim();
        var name = request.Body.Name.Trim();
        if (await repo.AnyAsync(x => x.Code == code && x.Id != request.Id, cancellationToken))
            return Result<SemesterListItemDto>.Failure("Mã học kỳ đã được dùng.", ErrorCodeEnum.DuplicateEntry);

        entity.Code = code;
        entity.Name = name;
        entity.StartDateUtc = request.Body.StartDateUtc;
        entity.EndDateUtc = request.Body.EndDateUtc;
        entity.UpdateEntity(userId);

        repo.Update(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update semester {SemesterId}", request.Id);
            throw;
        }

        return Result<SemesterListItemDto>.Success(
            new SemesterListItemDto(entity.Id, entity.Code, entity.Name, entity.StartDateUtc, entity.EndDateUtc),
            "Đã cập nhật học kỳ.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteSemesterCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteSemesterCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteSemesterCommandHandler> logger)
    : IRequestHandler<EgDeleteSemesterCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteSemesterCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<Semester>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        if (await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.SemesterId == request.Id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — còn ca thi thuộc học kỳ.", ErrorCodeEnum.BusinessRuleViolation);
        if (await unitOfWork.Repository<ExamClass>().AnyAsync(x => x.SemesterId == request.Id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — còn lớp học thuộc học kỳ.", ErrorCodeEnum.BusinessRuleViolation);

        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);
        repo.Update(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete semester {SemesterId}", request.Id);
            throw;
        }

        return Result<bool>.Success(true, "Đã xóa học kỳ.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgListExamSessionsQuery(Guid? SemesterId) : IRequest<Result<List<ExamSessionListItemDto>>>;
public sealed class EgListExamSessionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListExamSessionsQuery, Result<List<ExamSessionListItemDto>>>
{
    public async Task<Result<List<ExamSessionListItemDto>>> Handle(EgListExamSessionsQuery request, CancellationToken cancellationToken)
    {
        var q = unitOfWork.Repository<ExamSession>().GetQueryable().AsNoTracking();
        if (request.SemesterId.HasValue)
            q = q.Where(x => x.SemesterId == request.SemesterId.Value);

        var rows = await q
            .OrderByDescending(x => x.StartsAtUtc)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.StartsAtUtc,
                x.ExamDurationMinutes,
                x.EndsAtUtc,
                x.DeferredClassGrading,
                x.Topics.Count,
                x.Topics.SelectMany(t => t.Questions).Count(),
                x.Submissions.Count))
            .ToListAsync(cancellationToken);

        return Result<List<ExamSessionListItemDto>>.Success(rows, "OK");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateExamSessionCommand(CreateExamSessionRequest Body) : IRequest<Result<ExamSessionListItemDto>>;
public sealed class EgCreateExamSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateExamSessionCommandHandler> logger)
    : IRequestHandler<EgCreateExamSessionCommand, Result<ExamSessionListItemDto>>
{
    public async Task<Result<ExamSessionListItemDto>> Handle(EgCreateExamSessionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<Semester>().AnyAsync(x => x.Id == request.Body.SemesterId, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = request.Body.Code.Trim();
        var title = request.Body.Title.Trim();

        if (await unitOfWork.Repository<ExamSession>().AnyAsync(
                x => x.SemesterId == request.Body.SemesterId && x.Code == code, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Mã ca thi đã tồn tại trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamSession
        {
            SemesterId = request.Body.SemesterId,
            Code = code,
            Title = title,
            StartsAtUtc = request.Body.StartsAtUtc,
            ExamDurationMinutes = request.Body.ExamDurationMinutes,
            EndsAtUtc = request.Body.EndsAtUtc,
            DeferredClassGrading = request.Body.DeferredClassGrading,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);

        await unitOfWork.Repository<ExamSession>().AddAsync(entity, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create exam session {Code}", code);
            throw;
        }

        var dto = await unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.StartsAtUtc,
                x.ExamDurationMinutes,
                x.EndsAtUtc,
                x.DeferredClassGrading,
                x.Topics.Count,
                x.Topics.SelectMany(t => t.Questions).Count(),
                x.Submissions.Count))
            .FirstAsync(cancellationToken);

        return Result<ExamSessionListItemDto>.Success(dto, "Đã tạo ca thi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateExamSessionCommand(Guid Id, UpdateExamSessionRequest Body) : IRequest<Result<ExamSessionListItemDto>>;
public sealed class EgUpdateExamSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateExamSessionCommandHandler> logger)
    : IRequestHandler<EgUpdateExamSessionCommand, Result<ExamSessionListItemDto>>
{
    public async Task<Result<ExamSessionListItemDto>> Handle(EgUpdateExamSessionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamSession>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<ExamSessionListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (!await unitOfWork.Repository<Semester>().AnyAsync(x => x.Id == request.Body.SemesterId, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = request.Body.Code.Trim();
        if (await repo.AnyAsync(
                x => x.SemesterId == request.Body.SemesterId && x.Code == code && x.Id != request.Id, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Mã ca thi đã được dùng trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        entity.SemesterId = request.Body.SemesterId;
        entity.Code = code;
        entity.Title = request.Body.Title.Trim();
        entity.StartsAtUtc = request.Body.StartsAtUtc;
        entity.ExamDurationMinutes = request.Body.ExamDurationMinutes;
        entity.EndsAtUtc = request.Body.EndsAtUtc;
        entity.DeferredClassGrading = request.Body.DeferredClassGrading;
        entity.UpdateEntity(userId);

        repo.Update(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update exam session {SessionId}", request.Id);
            throw;
        }

        var dto = await unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.StartsAtUtc,
                x.ExamDurationMinutes,
                x.EndsAtUtc,
                x.DeferredClassGrading,
                x.Topics.Count,
                x.Topics.SelectMany(t => t.Questions).Count(),
                x.Submissions.Count))
            .FirstAsync(cancellationToken);
        return Result<ExamSessionListItemDto>.Success(dto, "Đã cập nhật ca thi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteExamSessionCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileServiceFactory fileServiceFactory,
    ILogger<EgDeleteExamSessionCommandHandler> logger)
    : IRequestHandler<EgDeleteExamSessionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteExamSessionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .Include(x => x.GradingPacks).ThenInclude(p => p.Assets)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (await unitOfWork.Repository<ExamSubmission>().AnyAsync(x => x.ExamSessionId == request.Id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — đã có bài nộp.", ErrorCodeEnum.BusinessRuleViolation);

        var fileService = fileServiceFactory.CreateFileService();
        foreach (var asset in entity.GradingPacks.SelectMany(p => p.Assets))
        {
            try
            {
                await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không xóa được file asset {Path}", asset.StorageRelativePath);
            }
        }

        foreach (var tc in entity.Topics.SelectMany(t => t.Questions).SelectMany(q => q.TestCases))
        {
            tc.SoftDeleteEntity(userId);
            tc.UpdateEntity(userId);
        }
        foreach (var q in entity.Topics.SelectMany(t => t.Questions))
        {
            q.SoftDeleteEntity(userId);
            q.UpdateEntity(userId);
        }
        foreach (var t in entity.Topics)
        {
            t.SoftDeleteEntity(userId);
            t.UpdateEntity(userId);
        }
        foreach (var asset in entity.GradingPacks.SelectMany(p => p.Assets))
        {
            asset.SoftDeleteEntity(userId);
            asset.UpdateEntity(userId);
        }
        foreach (var pack in entity.GradingPacks)
        {
            pack.SoftDeleteEntity(userId);
            pack.UpdateEntity(userId);
        }

        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete exam session {SessionId}", request.Id);
            throw;
        }

        return Result<bool>.Success(true, "Đã xóa ca thi và cấu trúc đề kèm theo.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateTopicCommand(Guid SessionId, CreateExamTopicRequest Body) : IRequest<Result<ExamTopicDetailDto>>;
public sealed class EgCreateTopicCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateTopicCommandHandler> logger)
    : IRequestHandler<EgCreateTopicCommand, Result<ExamTopicDetailDto>>
{
    public async Task<Result<ExamTopicDetailDto>> Handle(EgCreateTopicCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.Id == request.SessionId, cancellationToken))
            return Result<ExamTopicDetailDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var title = request.Body.Title.Trim();
        var entity = new ExamTopic
        {
            ExamSessionId = request.SessionId,
            Title = title,
            SortOrder = request.Body.SortOrder,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamTopic>().AddAsync(entity, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create topic in session {SessionId}", request.SessionId);
            throw;
        }

        return Result<ExamTopicDetailDto>.Success(
            new ExamTopicDetailDto(entity.Id, entity.Title, entity.SortOrder, Array.Empty<ExamQuestionDetailDto>()),
            "Đã tạo chủ đề.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateTopicCommand(Guid TopicId, UpdateExamTopicRequest Body) : IRequest<Result<ExamTopicDetailDto>>;
public sealed class EgUpdateTopicCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateTopicCommandHandler> logger)
    : IRequestHandler<EgUpdateTopicCommand, Result<ExamTopicDetailDto>>
{
    public async Task<Result<ExamTopicDetailDto>> Handle(EgUpdateTopicCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamTopic>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.TopicId, cancellationToken: cancellationToken);
        if (entity == null) return Result<ExamTopicDetailDto>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        entity.Title = request.Body.Title.Trim();
        entity.SortOrder = request.Body.SortOrder;
        entity.UpdateEntity(userId);
        repo.Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update topic {TopicId}", request.TopicId);
            throw;
        }

        var questions = await unitOfWork.Repository<ExamQuestion>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.ExamTopicId == request.TopicId)
            .OrderBy(x => x.Label)
            .Select(x => new ExamQuestionDetailDto(
                x.Id,
                x.Label,
                x.Title,
                x.MaxScore,
                x.TestCases.OrderBy(tc => tc.SortOrder)
                    .Select(tc => new ExamTestCaseDetailDto(tc.Id, tc.Name, tc.MaxPoints, tc.SortOrder))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Result<ExamTopicDetailDto>.Success(
            new ExamTopicDetailDto(entity.Id, entity.Title, entity.SortOrder, questions),
            "Đã cập nhật chủ đề.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteTopicCommand(Guid TopicId) : IRequest<Result<bool>>;
public sealed class EgDeleteTopicCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteTopicCommandHandler> logger)
    : IRequestHandler<EgDeleteTopicCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamTopic>().GetQueryable()
            .Include(x => x.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == request.TopicId, cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        foreach (var tc in entity.Questions.SelectMany(q => q.TestCases))
        {
            tc.SoftDeleteEntity(userId);
            tc.UpdateEntity(userId);
        }
        foreach (var q in entity.Questions)
        {
            q.SoftDeleteEntity(userId);
            q.UpdateEntity(userId);
        }
        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete topic {TopicId}", request.TopicId);
            throw;
        }
        return Result<bool>.Success(true, "Đã xóa chủ đề.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateQuestionCommand(Guid TopicId, CreateExamQuestionRequest Body) : IRequest<Result<ExamQuestionDetailDto>>;
public sealed class EgCreateQuestionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateQuestionCommandHandler> logger)
    : IRequestHandler<EgCreateQuestionCommand, Result<ExamQuestionDetailDto>>
{
    public async Task<Result<ExamQuestionDetailDto>> Handle(EgCreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<ExamTopic>().AnyAsync(x => x.Id == request.TopicId, cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        var label = request.Body.Label.Trim();
        if (await unitOfWork.Repository<ExamQuestion>().AnyAsync(
                x => x.ExamTopicId == request.TopicId && x.Label.ToLower() == label.ToLower(), cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Label câu hỏi đã tồn tại trong chủ đề.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamQuestion
        {
            ExamTopicId = request.TopicId,
            Label = label,
            Title = request.Body.Title.Trim(),
            MaxScore = request.Body.MaxScore,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamQuestion>().AddAsync(entity, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create question in topic {TopicId}", request.TopicId);
            throw;
        }

        return Result<ExamQuestionDetailDto>.Success(
            new ExamQuestionDetailDto(entity.Id, entity.Label, entity.Title, entity.MaxScore, Array.Empty<ExamTestCaseDetailDto>()),
            "Đã tạo câu hỏi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateQuestionCommand(Guid QuestionId, UpdateExamQuestionRequest Body) : IRequest<Result<ExamQuestionDetailDto>>;
public sealed class EgUpdateQuestionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateQuestionCommandHandler> logger)
    : IRequestHandler<EgUpdateQuestionCommand, Result<ExamQuestionDetailDto>>
{
    public async Task<Result<ExamQuestionDetailDto>> Handle(EgUpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamQuestion>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.QuestionId, cancellationToken: cancellationToken);
        if (entity == null) return Result<ExamQuestionDetailDto>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        var label = request.Body.Label.Trim();
        if (await repo.AnyAsync(
                x => x.ExamTopicId == entity.ExamTopicId && x.Label.ToLower() == label.ToLower() && x.Id != request.QuestionId, cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Label đã được dùng trong chủ đề.", ErrorCodeEnum.DuplicateEntry);

        entity.Label = label;
        entity.Title = request.Body.Title.Trim();
        entity.MaxScore = request.Body.MaxScore;
        entity.UpdateEntity(userId);
        repo.Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update question {QuestionId}", request.QuestionId);
            throw;
        }

        var testCases = await unitOfWork.Repository<ExamTestCase>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.ExamQuestionId == request.QuestionId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ExamTestCaseDetailDto(x.Id, x.Name, x.MaxPoints, x.SortOrder))
            .ToListAsync(cancellationToken);

        return Result<ExamQuestionDetailDto>.Success(
            new ExamQuestionDetailDto(entity.Id, entity.Label, entity.Title, entity.MaxScore, testCases),
            "Đã cập nhật câu hỏi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteQuestionCommand(Guid QuestionId) : IRequest<Result<bool>>;
public sealed class EgDeleteQuestionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteQuestionCommandHandler> logger)
    : IRequestHandler<EgDeleteQuestionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamQuestion>().GetQueryable()
            .Include(x => x.TestCases)
            .FirstOrDefaultAsync(x => x.Id == request.QuestionId, cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        foreach (var tc in entity.TestCases)
        {
            tc.SoftDeleteEntity(userId);
            tc.UpdateEntity(userId);
        }
        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete question {QuestionId}", request.QuestionId);
            throw;
        }
        return Result<bool>.Success(true, "Đã xóa câu hỏi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateTestCaseCommand(Guid QuestionId, CreateExamTestCaseRequest Body) : IRequest<Result<ExamTestCaseDetailDto>>;
public sealed class EgCreateTestCaseCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateTestCaseCommandHandler> logger)
    : IRequestHandler<EgCreateTestCaseCommand, Result<ExamTestCaseDetailDto>>
{
    public async Task<Result<ExamTestCaseDetailDto>> Handle(EgCreateTestCaseCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<ExamQuestion>().AnyAsync(x => x.Id == request.QuestionId, cancellationToken))
            return Result<ExamTestCaseDetailDto>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        var entity = new ExamTestCase
        {
            ExamQuestionId = request.QuestionId,
            Name = request.Body.Name.Trim(),
            MaxPoints = request.Body.MaxPoints,
            SortOrder = request.Body.SortOrder,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamTestCase>().AddAsync(entity, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create testcase for question {QuestionId}", request.QuestionId);
            throw;
        }

        return Result<ExamTestCaseDetailDto>.Success(
            new ExamTestCaseDetailDto(entity.Id, entity.Name, entity.MaxPoints, entity.SortOrder),
            "Đã tạo testcase.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateTestCaseCommand(Guid TestCaseId, UpdateExamTestCaseRequest Body) : IRequest<Result<ExamTestCaseDetailDto>>;
public sealed class EgUpdateTestCaseCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateTestCaseCommandHandler> logger)
    : IRequestHandler<EgUpdateTestCaseCommand, Result<ExamTestCaseDetailDto>>
{
    public async Task<Result<ExamTestCaseDetailDto>> Handle(EgUpdateTestCaseCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamTestCase>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.TestCaseId, cancellationToken: cancellationToken);
        if (entity == null) return Result<ExamTestCaseDetailDto>.Failure("Không tìm thấy testcase.", ErrorCodeEnum.NotFound);

        entity.Name = request.Body.Name.Trim();
        entity.MaxPoints = request.Body.MaxPoints;
        entity.SortOrder = request.Body.SortOrder;
        entity.UpdateEntity(userId);
        repo.Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update testcase {TestCaseId}", request.TestCaseId);
            throw;
        }
        return Result<ExamTestCaseDetailDto>.Success(
            new ExamTestCaseDetailDto(entity.Id, entity.Name, entity.MaxPoints, entity.SortOrder),
            "Đã cập nhật testcase.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteTestCaseCommand(Guid TestCaseId) : IRequest<Result<bool>>;
public sealed class EgDeleteTestCaseCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteTestCaseCommandHandler> logger)
    : IRequestHandler<EgDeleteTestCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteTestCaseCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamTestCase>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.TestCaseId, cancellationToken: cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy testcase.", ErrorCodeEnum.NotFound);

        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);
        repo.Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete testcase {TestCaseId}", request.TestCaseId);
            throw;
        }
        return Result<bool>.Success(true, "Đã xóa testcase.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgListGradingPacksQuery(Guid SessionId) : IRequest<Result<List<ExamGradingPackListItemDto>>>;
public sealed class EgListGradingPacksQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListGradingPacksQuery, Result<List<ExamGradingPackListItemDto>>>
{
    public async Task<Result<List<ExamGradingPackListItemDto>>> Handle(EgListGradingPacksQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.Id == request.SessionId, cancellationToken))
            return Result<List<ExamGradingPackListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.ExamSessionId == request.SessionId)
            .OrderByDescending(x => x.Version)
            .Select(x => new ExamGradingPackListItemDto(x.Id, x.Version, x.Label, x.IsActive, x.Assets.Count))
            .ToListAsync(cancellationToken);
        return Result<List<ExamGradingPackListItemDto>>.Success(rows, "OK");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateGradingPackCommand(Guid SessionId, CreateGradingPackRequest Body) : IRequest<Result<ExamGradingPackListItemDto>>;
public sealed class EgCreateGradingPackCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateGradingPackCommandHandler> logger)
    : IRequestHandler<EgCreateGradingPackCommand, Result<ExamGradingPackListItemDto>>
{
    public async Task<Result<ExamGradingPackListItemDto>> Handle(EgCreateGradingPackCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.Id == request.SessionId, cancellationToken))
            return Result<ExamGradingPackListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var label = request.Body.Label.Trim();
        int version;
        if (request.Body.Version is null or <= 0)
        {
            var max = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
                .Where(x => x.ExamSessionId == request.SessionId)
                .Select(x => (int?)x.Version)
                .MaxAsync(cancellationToken);
            version = (max ?? 0) + 1;
        }
        else
        {
            version = request.Body.Version.Value;
            if (await unitOfWork.Repository<ExamGradingPack>()
                    .AnyAsync(x => x.ExamSessionId == request.SessionId && x.Version == version, cancellationToken))
                return Result<ExamGradingPackListItemDto>.Failure("Version đã tồn tại.", ErrorCodeEnum.DuplicateEntry);
        }

        if (request.Body.IsActive)
        {
            var others = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
                .Where(x => x.ExamSessionId == request.SessionId && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var p in others)
            {
                p.IsActive = false;
                p.UpdateEntity(userId);
            }
        }

        var entity = new ExamGradingPack
        {
            ExamSessionId = request.SessionId,
            Version = version,
            Label = label,
            IsActive = request.Body.IsActive,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamGradingPack>().AddAsync(entity, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create grading pack for session {SessionId}", request.SessionId);
            throw;
        }
        return Result<ExamGradingPackListItemDto>.Success(
            new ExamGradingPackListItemDto(entity.Id, entity.Version, entity.Label, entity.IsActive, 0),
            "Đã tạo grading pack.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateGradingPackCommand(Guid PackId, UpdateGradingPackRequest Body) : IRequest<Result<ExamGradingPackListItemDto>>;
public sealed class EgUpdateGradingPackCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateGradingPackCommandHandler> logger)
    : IRequestHandler<EgUpdateGradingPackCommand, Result<ExamGradingPackListItemDto>>
{
    public async Task<Result<ExamGradingPackListItemDto>> Handle(EgUpdateGradingPackCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
            .Include(x => x.Assets)
            .FirstOrDefaultAsync(x => x.Id == request.PackId, cancellationToken);
        if (entity == null) return Result<ExamGradingPackListItemDto>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        if (request.Body.IsActive)
        {
            var others = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
                .Where(x => x.ExamSessionId == entity.ExamSessionId && x.Id != entity.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var p in others)
            {
                p.IsActive = false;
                p.UpdateEntity(userId);
            }
        }

        entity.Label = request.Body.Label.Trim();
        entity.IsActive = request.Body.IsActive;
        entity.UpdateEntity(userId);
        unitOfWork.Repository<ExamGradingPack>().Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update grading pack {PackId}", request.PackId);
            throw;
        }

        return Result<ExamGradingPackListItemDto>.Success(
            new ExamGradingPackListItemDto(entity.Id, entity.Version, entity.Label, entity.IsActive, entity.Assets.Count),
            "Đã cập nhật pack.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteGradingPackCommand(Guid PackId) : IRequest<Result<bool>>;
public sealed class EgDeleteGradingPackCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileServiceFactory fileServiceFactory,
    ILogger<EgDeleteGradingPackCommandHandler> logger)
    : IRequestHandler<EgDeleteGradingPackCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteGradingPackCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamGradingPack>().GetQueryable()
            .Include(x => x.Assets)
            .FirstOrDefaultAsync(x => x.Id == request.PackId, cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        var fileService = fileServiceFactory.CreateFileService();
        foreach (var asset in entity.Assets)
        {
            try
            {
                await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không xóa file pack asset {Path}", asset.StorageRelativePath);
            }

            asset.SoftDeleteEntity(userId);
            asset.UpdateEntity(userId);
        }

        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);
        unitOfWork.Repository<ExamGradingPack>().Update(entity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete grading pack {PackId}", request.PackId);
            throw;
        }
        return Result<bool>.Success(true, "Đã xóa pack.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreatePackAssetCommand(Guid PackId, ExamPackAssetKind Kind, IFormFile File) : IRequest<Result<ExamPackAssetListItemDto>>;
public sealed class EgCreatePackAssetCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileServiceFactory fileServiceFactory,
    ILogger<EgCreatePackAssetCommandHandler> logger)
    : IRequestHandler<EgCreatePackAssetCommand, Result<ExamPackAssetListItemDto>>
{
    public async Task<Result<ExamPackAssetListItemDto>> Handle(EgCreatePackAssetCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var pack = await unitOfWork.Repository<ExamGradingPack>()
            .GetFirstOrDefaultAsync(x => x.Id == request.PackId, cancellationToken: cancellationToken);
        if (pack == null) return Result<ExamPackAssetListItemDto>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        var safeName = Path.GetFileName(request.File.FileName);
        if (string.IsNullOrEmpty(safeName)) safeName = "asset.bin";

        var fileService = fileServiceFactory.CreateFileService();
        var subDir = Path.Combine("exam-pack-assets", request.PackId.ToString("N"));
        var storedName = $"{Guid.NewGuid():N}_{safeName}";

        string relativePath;
        try
        {
            relativePath = await fileService.UploadFileAsync(request.File, storedName, subDir, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload pack asset thất bại");
            return Result<ExamPackAssetListItemDto>.Failure("Upload thất bại.", ErrorCodeEnum.FileUploadFailed);
        }

        var asset = new ExamPackAsset
        {
            ExamGradingPackId = request.PackId,
            Kind = request.Kind,
            StorageRelativePath = relativePath,
            OriginalFileName = request.File.FileName,
            Status = EntityStatusEnum.Active
        };
        asset.InitializeEntity(userId);
        await unitOfWork.Repository<ExamPackAsset>().AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ExamPackAssetListItemDto>.Success(
            new ExamPackAssetListItemDto(asset.Id, asset.ExamGradingPackId, (int)asset.Kind, asset.StorageRelativePath, asset.OriginalFileName),
            "Đã thêm asset.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeletePackAssetCommand(Guid AssetId) : IRequest<Result<bool>>;
public sealed class EgDeletePackAssetCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileServiceFactory fileServiceFactory,
    ILogger<EgDeletePackAssetCommandHandler> logger)
    : IRequestHandler<EgDeletePackAssetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeletePackAssetCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamPackAsset>();
        var asset = await repo.GetFirstOrDefaultAsync(x => x.Id == request.AssetId, cancellationToken: cancellationToken);
        if (asset == null) return Result<bool>.Failure("Không tìm thấy asset.", ErrorCodeEnum.NotFound);

        var fileService = fileServiceFactory.CreateFileService();
        try
        {
            await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không xóa file asset {Path}", asset.StorageRelativePath);
        }

        asset.SoftDeleteEntity(userId);
        asset.UpdateEntity(userId);
        repo.Update(asset);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, "Đã xóa asset.");
    }
}

public record EgGetExamSessionQuery(Guid Id) : IRequest<Result<ExamSessionDetailDto>>;
public sealed class EgGetExamSessionQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgGetExamSessionQuery, Result<ExamSessionDetailDto>>
{
    public async Task<Result<ExamSessionDetailDto>> Handle(EgGetExamSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .AsNoTracking()
            .Include(x => x.Semester)
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (session == null)
            return Result<ExamSessionDetailDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var topics = session.Topics
            .OrderBy(t => t.SortOrder)
            .Select(t => new ExamTopicDetailDto(
                t.Id,
                t.Title,
                t.SortOrder,
                t.Questions
                    .OrderBy(q => q.Label)
                    .Select(q => new ExamQuestionDetailDto(
                        q.Id,
                        q.Label,
                        q.Title,
                        q.MaxScore,
                        q.TestCases
                            .OrderBy(tc => tc.SortOrder)
                            .Select(tc => new ExamTestCaseDetailDto(tc.Id, tc.Name, tc.MaxPoints, tc.SortOrder))
                            .ToList()))
                    .ToList()))
            .ToList();

        var dto = new ExamSessionDetailDto(
            session.Id,
            session.Code,
            session.Title,
            session.SemesterId,
            session.Semester.Code,
            session.StartsAtUtc,
            session.ExamDurationMinutes,
            session.EndsAtUtc,
            session.DeferredClassGrading,
            topics);

        return Result<ExamSessionDetailDto>.Success(dto, "OK");
    }
}

public record EgListSubmissionsQuery(Guid ExamSessionId, Guid? ExamSessionClassId) : IRequest<Result<List<ExamSubmissionListItemDto>>>;
public sealed class EgListSubmissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListSubmissionsQuery, Result<List<ExamSubmissionListItemDto>>>
{
    public async Task<Result<List<ExamSubmissionListItemDto>>> Handle(EgListSubmissionsQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.Id == request.ExamSessionId, cancellationToken))
            return Result<List<ExamSubmissionListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var query = unitOfWork.Repository<ExamSubmission>()
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.ExamSessionId == request.ExamSessionId);

        if (request.ExamSessionClassId.HasValue)
            query = query.Where(x => x.ExamSessionClassId == request.ExamSessionClassId.Value);

        var rows = await query
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new ExamSubmissionListItemDto(
                x.Id,
                x.ExamSessionId,
                x.ExamSessionClassId,
                x.ExamSessionClass != null ? x.ExamSessionClass.ExamClass.Code : null,
                x.StudentCode,
                x.StudentName,
                x.WorkflowStatus.ToString(),
                x.SubmittedAtUtc,
                x.TotalScore))
            .ToListAsync(cancellationToken);

        return Result<List<ExamSubmissionListItemDto>>.Success(rows, "OK");
    }
}

public record EgGetSubmissionQuery(Guid Id) : IRequest<Result<ExamSubmissionDetailDto>>;
public sealed class EgGetSubmissionQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgGetSubmissionQuery, Result<ExamSubmissionDetailDto>>
{
    public async Task<Result<ExamSubmissionDetailDto>> Handle(EgGetSubmissionQuery request, CancellationToken cancellationToken)
    {
        var submission = await unitOfWork.Repository<ExamSubmission>()
            .GetQueryable()
            .AsNoTracking()
            .Include(x => x.ExamSession)
            .Include(x => x.ExamSessionClass).ThenInclude(c => c!.ExamClass)
            .Include(x => x.SubmissionFiles)
            .Include(x => x.QuestionScores).ThenInclude(qs => qs.ExamQuestion)
            .Include(x => x.Result).ThenInclude(r => r!.Details).ThenInclude(d => d.TestCase)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (submission == null)
            return Result<ExamSubmissionDetailDto>.Failure("Không tìm thấy bài nộp.", ErrorCodeEnum.NotFound);

        var questionScores = submission.QuestionScores
            .OrderBy(x => x.ExamQuestion.Label)
            .Select(x => new ExamQuestionScoreDto(
                x.ExamQuestionId,
                x.ExamQuestion.Label,
                x.Score,
                x.MaxScore,
                x.Summary))
            .ToList();

        var resultDetails = submission.Result?.Details
            .OrderBy(x => x.TestCase.SortOrder)
            .Select(x => new ResultDetail(
                x.TestCase.Name,
                x.Passed,
                x.Score,
                x.ResponseTime,
                x.ErrorMessage,
                null))
            .ToList() ?? new List<ResultDetail>();

        var files = submission.SubmissionFiles
            .OrderBy(x => x.QuestionLabel)
            .Select(x => new SubmissionFileDto(x.QuestionLabel, x.StorageRelativePath, x.OriginalFileName))
            .ToList();

        var dto = new ExamSubmissionDetailDto(
            submission.Id,
            submission.ExamSessionId,
            submission.ExamSession.Code,
            submission.ExamSessionClassId,
            submission.ExamSessionClass?.ExamClass.Code,
            submission.StudentCode,
            submission.StudentName,
            submission.WorkflowStatus.ToString(),
            submission.SubmittedAtUtc,
            submission.TotalScore,
            files,
            questionScores,
            resultDetails);

        return Result<ExamSubmissionDetailDto>.Success(dto, "OK");
    }
}

public record EgCreateSubmissionCommand(
    Guid ExamSessionId,
    string StudentCode,
    string? StudentName,
    Guid? ExamSessionClassId,
    IFormFile Q1Zip,
    IFormFile Q2Zip,
    bool BypassExamWindow) : IRequest<Result<Guid>>;
public sealed class EgCreateSubmissionCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgCreateSubmissionCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(EgCreateSubmissionCommand request, CancellationToken cancellationToken)
        => grading.CreateSubmissionWithZipAsync(
            request.ExamSessionId,
            request.StudentCode,
            request.StudentName,
            request.Q1Zip,
            request.Q2Zip,
            request.BypassExamWindow,
            request.ExamSessionClassId,
            cancellationToken);
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgListExamClassesQuery(Guid SemesterId) : IRequest<Result<List<ExamClassListItemDto>>>;
public sealed class EgListExamClassesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListExamClassesQuery, Result<List<ExamClassListItemDto>>>
{
    public async Task<Result<List<ExamClassListItemDto>>> Handle(EgListExamClassesQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<Semester>().AnyAsync(x => x.Id == request.SemesterId, cancellationToken))
            return Result<List<ExamClassListItemDto>>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var rows = await unitOfWork.Repository<ExamClass>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.SemesterId == request.SemesterId)
            .OrderBy(x => x.Code)
            .Select(x => new ExamClassListItemDto(x.Id, x.SemesterId, x.Code, x.Name, x.MaxStudents))
            .ToListAsync(cancellationToken);
        return Result<List<ExamClassListItemDto>>.Success(rows, "OK");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateExamClassCommand(Guid SemesterId, CreateExamClassRequest Body) : IRequest<Result<ExamClassListItemDto>>;
public sealed class EgCreateExamClassCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateExamClassCommandHandler> logger)
    : IRequestHandler<EgCreateExamClassCommand, Result<ExamClassListItemDto>>
{
    public async Task<Result<ExamClassListItemDto>> Handle(EgCreateExamClassCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        if (!await unitOfWork.Repository<Semester>().AnyAsync(x => x.Id == request.SemesterId, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = request.Body.Code.Trim();
        var name = request.Body.Name.Trim();
        var max = request.Body.MaxStudents <= 0 ? 35 : request.Body.MaxStudents;

        if (await unitOfWork.Repository<ExamClass>().AnyAsync(x => x.SemesterId == request.SemesterId && x.Code == code, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Mã lớp đã tồn tại trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamClass
        {
            SemesterId = request.SemesterId,
            Code = code,
            Name = name,
            MaxStudents = max,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamClass>().AddAsync(entity, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create exam class {Code}", code);
            throw;
        }

        return Result<ExamClassListItemDto>.Success(
            new ExamClassListItemDto(entity.Id, entity.SemesterId, entity.Code, entity.Name, entity.MaxStudents),
            "Đã tạo lớp.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgUpdateExamClassCommand(Guid Id, UpdateExamClassRequest Body) : IRequest<Result<ExamClassListItemDto>>;
public sealed class EgUpdateExamClassCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgUpdateExamClassCommandHandler> logger)
    : IRequestHandler<EgUpdateExamClassCommand, Result<ExamClassListItemDto>>
{
    public async Task<Result<ExamClassListItemDto>> Handle(EgUpdateExamClassCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamClass>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<ExamClassListItemDto>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);

        var code = request.Body.Code.Trim();
        var name = request.Body.Name.Trim();
        if (await repo.AnyAsync(x => x.SemesterId == entity.SemesterId && x.Code == code && x.Id != request.Id, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Mã lớp đã được dùng.", ErrorCodeEnum.DuplicateEntry);

        var usedExpected = await unitOfWork.Repository<ExamSessionClass>().AnyAsync(
            x => x.ExamClassId == request.Id && x.ExpectedStudentCount > request.Body.MaxStudents,
            cancellationToken);
        if (usedExpected)
            return Result<ExamClassListItemDto>.Failure(
                "Không giảm MaxStudents xuống thấp hơn ExpectedStudentCount của một ca đã gắn lớp.",
                ErrorCodeEnum.BusinessRuleViolation);

        entity.Code = code;
        entity.Name = name;
        entity.MaxStudents = request.Body.MaxStudents;
        entity.UpdateEntity(userId);
        repo.Update(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update class {ClassId}", request.Id);
            throw;
        }

        return Result<ExamClassListItemDto>.Success(
            new ExamClassListItemDto(entity.Id, entity.SemesterId, entity.Code, entity.Name, entity.MaxStudents),
            "Đã cập nhật lớp.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteExamClassCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamClassCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteExamClassCommandHandler> logger)
    : IRequestHandler<EgDeleteExamClassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteExamClassCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var repo = unitOfWork.Repository<ExamClass>();
        var entity = await repo.GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);

        if (await unitOfWork.Repository<ExamSessionClass>().AnyAsync(x => x.ExamClassId == request.Id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — lớp đã gắn vào ca thi.", ErrorCodeEnum.BusinessRuleViolation);

        entity.SoftDeleteEntity(userId);
        entity.UpdateEntity(userId);
        repo.Update(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete class {ClassId}", request.Id);
            throw;
        }
        return Result<bool>.Success(true, "Đã xóa lớp.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgListExamSessionClassesQuery(Guid SessionId) : IRequest<Result<List<ExamSessionClassListItemDto>>>;
public sealed class EgListExamSessionClassesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EgListExamSessionClassesQuery, Result<List<ExamSessionClassListItemDto>>>
{
    public async Task<Result<List<ExamSessionClassListItemDto>>> Handle(EgListExamSessionClassesQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<ExamSession>().AnyAsync(x => x.Id == request.SessionId, cancellationToken))
            return Result<List<ExamSessionClassListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await unitOfWork.Repository<ExamSessionClass>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.ExamSessionId == request.SessionId)
            .Include(x => x.ExamClass)
            .OrderBy(x => x.ExamClass.Code)
            .Select(x => new
            {
                x.Id,
                x.ExamSessionId,
                x.ExamClassId,
                ClassCode = x.ExamClass.Code,
                ClassName = x.ExamClass.Name,
                x.ExpectedStudentCount,
                x.BatchStatus
            })
            .ToListAsync(cancellationToken);

        var classIds = rows.Select(r => r.Id).ToList();
        var subs = await unitOfWork.Repository<ExamSubmission>().GetQueryable()
            .AsNoTracking()
            .Where(s => s.ExamSessionClassId != null && classIds.Contains(s.ExamSessionClassId.Value))
            .Include(s => s.SubmissionFiles)
            .ToListAsync(cancellationToken);

        var byClass = subs.GroupBy(s => s.ExamSessionClassId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var dtos = rows.Select(x =>
        {
            var list = byClass.GetValueOrDefault(x.Id) ?? new List<ExamSubmission>();
            var total = list.Count;
            var ready = list.Count(s =>
                s.SubmissionFiles.Any(f => f.QuestionLabel.Equals("Q1", StringComparison.OrdinalIgnoreCase)) &&
                s.SubmissionFiles.Any(f => f.QuestionLabel.Equals("Q2", StringComparison.OrdinalIgnoreCase)) &&
                s.WorkflowStatus == ExamSubmissionStatus.Pending &&
                s.TotalScore == null);
            return new ExamSessionClassListItemDto(
                x.Id,
                x.ExamSessionId,
                x.ExamClassId,
                x.ClassCode,
                x.ClassName,
                x.ExpectedStudentCount,
                x.BatchStatus.ToString(),
                ready,
                total);
        }).ToList();
        return Result<List<ExamSessionClassListItemDto>>.Success(dtos, "OK");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgCreateExamSessionClassCommand(Guid SessionId, CreateExamSessionClassRequest Body) : IRequest<Result<ExamSessionClassListItemDto>>;
public sealed class EgCreateExamSessionClassCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgCreateExamSessionClassCommandHandler> logger)
    : IRequestHandler<EgCreateExamSessionClassCommand, Result<ExamSessionClassListItemDto>>
{
    public async Task<Result<ExamSessionClassListItemDto>> Handle(EgCreateExamSessionClassCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var session = await unitOfWork.Repository<ExamSession>()
            .GetFirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken: cancellationToken);
        if (session == null) return Result<ExamSessionClassListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var examClass = await unitOfWork.Repository<ExamClass>()
            .GetFirstOrDefaultAsync(x => x.Id == request.Body.ExamClassId, cancellationToken: cancellationToken);
        if (examClass == null) return Result<ExamSessionClassListItemDto>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);
        if (examClass.SemesterId != session.SemesterId)
            return Result<ExamSessionClassListItemDto>.Failure("Lớp không cùng học kỳ với ca thi.", ErrorCodeEnum.ValidationFailed);

        if (await unitOfWork.Repository<ExamSessionClass>().AnyAsync(
                x => x.ExamSessionId == request.SessionId && x.ExamClassId == request.Body.ExamClassId, cancellationToken))
            return Result<ExamSessionClassListItemDto>.Failure("Ca thi đã gắn lớp này rồi.", ErrorCodeEnum.DuplicateEntry);

        var expected = request.Body.ExpectedStudentCount <= 0 ? examClass.MaxStudents : request.Body.ExpectedStudentCount;
        if (expected > examClass.MaxStudents)
            return Result<ExamSessionClassListItemDto>.Failure(
                $"ExpectedStudentCount không được vượt MaxStudents của lớp ({examClass.MaxStudents}).",
                ErrorCodeEnum.ValidationFailed);

        var entity = new ExamSessionClass
        {
            ExamSessionId = request.SessionId,
            ExamClassId = request.Body.ExamClassId,
            ExpectedStudentCount = expected,
            BatchStatus = ClassGradingBatchStatus.CollectingSubmissions,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(userId);
        await unitOfWork.Repository<ExamSessionClass>().AddAsync(entity, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session-class for session {SessionId}", request.SessionId);
            throw;
        }

        var dto = await unitOfWork.Repository<ExamSessionClass>().GetQueryable()
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(x => new ExamSessionClassListItemDto(
                x.Id,
                x.ExamSessionId,
                x.ExamClassId,
                x.ExamClass.Code,
                x.ExamClass.Name,
                x.ExpectedStudentCount,
                x.BatchStatus.ToString(),
                0,
                0))
            .FirstAsync(cancellationToken);
        return Result<ExamSessionClassListItemDto>.Success(dto, "Đã gắn lớp vào ca thi.");
    }
}

[Authorize(Roles = "Instructor,SystemAdmin")]
public record EgDeleteExamSessionClassCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamSessionClassCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<EgDeleteExamSessionClassCommandHandler> logger)
    : IRequestHandler<EgDeleteExamSessionClassCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EgDeleteExamSessionClassCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) throw new UnauthorizedAccessException();

        var entity = await unitOfWork.Repository<ExamSessionClass>()
            .GetFirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        if (entity == null) return Result<bool>.Failure("Không tìm thấy bản ghi lớp trong ca.", ErrorCodeEnum.NotFound);

        if (await unitOfWork.Repository<ExamSubmission>().AnyAsync(x => x.ExamSessionClassId == request.Id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — đã có bài nộp cho lớp trong ca này.", ErrorCodeEnum.BusinessRuleViolation);

        unitOfWork.Repository<ExamSessionClass>().Delete(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete session-class {SessionClassId} by user {UserId}", request.Id, userId);
            throw;
        }
        return Result<bool>.Success(true, "Đã gỡ lớp khỏi ca thi.");
    }
}

public record EgStartClassBatchGradingCommand(Guid Id, StartClassBatchGradingRequest Body) : IRequest<Result<StartClassBatchGradingResponseDto>>;
public sealed class EgStartClassBatchGradingCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgStartClassBatchGradingCommand, Result<StartClassBatchGradingResponseDto>>
{
    public Task<Result<StartClassBatchGradingResponseDto>> Handle(EgStartClassBatchGradingCommand request, CancellationToken cancellationToken)
        => grading.StartClassBatchGradingAsync(request.Id, request.Body, cancellationToken);
}

public record EgReplaceSubmissionFileCommand(Guid Id, string QuestionLabel, IFormFile ZipFile) : IRequest<Result<bool>>;
public sealed class EgReplaceSubmissionFileCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgReplaceSubmissionFileCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgReplaceSubmissionFileCommand request, CancellationToken cancellationToken)
        => grading.ReplaceSubmissionFileAsync(request.Id, request.QuestionLabel, request.ZipFile, cancellationToken);
}

public record EgTriggerRegradeCommand(Guid Id) : IRequest<Result<TriggerRegradeResponseDto>>;
public sealed class EgTriggerRegradeCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgTriggerRegradeCommand, Result<TriggerRegradeResponseDto>>
{
    public Task<Result<TriggerRegradeResponseDto>> Handle(EgTriggerRegradeCommand request, CancellationToken cancellationToken)
        => grading.TriggerRegradeAsync(request.Id, cancellationToken);
}
