using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class ExamGradingAdminService : IExamGradingAdminService
{
    private readonly PRN232_G9_AutoGradingToolDbContext _db;
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly ILogger<ExamGradingAdminService> _logger;

    public ExamGradingAdminService(
        PRN232_G9_AutoGradingToolDbContext db,
        IFileServiceFactory fileServiceFactory,
        ILogger<ExamGradingAdminService> logger)
    {
        _db = db;
        _fileServiceFactory = fileServiceFactory;
        _logger = logger;
    }

    public async Task<Result<SemesterListItemDto>> CreateSemesterAsync(CreateSemesterRequest req, CancellationToken cancellationToken = default)
    {
        var code = req.Code.Trim();
        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Result<SemesterListItemDto>.Failure("Code và Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (await _db.Semesters.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<SemesterListItemDto>.Failure("Mã học kỳ đã tồn tại.", ErrorCodeEnum.DuplicateEntry);

        var entity = new Semester
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            StartDateUtc = req.StartDateUtc,
            EndDateUtc = req.EndDateUtc,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.Semesters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SemesterListItemDto>.Success(
            new SemesterListItemDto(entity.Id, entity.Code, entity.Name, entity.StartDateUtc, entity.EndDateUtc),
            "Đã tạo học kỳ.");
    }

    public async Task<Result<SemesterListItemDto>> UpdateSemesterAsync(Guid id, UpdateSemesterRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Semesters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return Result<SemesterListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = req.Code.Trim();
        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Result<SemesterListItemDto>.Failure("Code và Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (await _db.Semesters.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            return Result<SemesterListItemDto>.Failure("Mã học kỳ đã được dùng.", ErrorCodeEnum.DuplicateEntry);

        entity.Code = code;
        entity.Name = name;
        entity.StartDateUtc = req.StartDateUtc;
        entity.EndDateUtc = req.EndDateUtc;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SemesterListItemDto>.Success(
            new SemesterListItemDto(entity.Id, entity.Code, entity.Name, entity.StartDateUtc, entity.EndDateUtc),
            "Đã cập nhật học kỳ.");
    }

    public async Task<Result<bool>> DeleteSemesterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Semesters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        if (await _db.ExamSessions.AnyAsync(x => x.SemesterId == id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — còn ca thi thuộc học kỳ.", ErrorCodeEnum.BusinessRuleViolation);

        if (await _db.ExamClasses.AnyAsync(x => x.SemesterId == id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — còn lớp học thuộc học kỳ.", ErrorCodeEnum.BusinessRuleViolation);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa học kỳ.");
    }

    public async Task<Result<ExamSessionListItemDto>> CreateExamSessionAsync(CreateExamSessionRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Semesters.AnyAsync(x => x.Id == req.SemesterId, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = req.Code.Trim();
        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(title))
            return Result<ExamSessionListItemDto>.Failure("Code và Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (req.ExamDurationMinutes <= 0)
            return Result<ExamSessionListItemDto>.Failure("examDurationMinutes phải > 0.", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamSessions.AnyAsync(x => x.SemesterId == req.SemesterId && x.Code == code, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Mã ca thi đã tồn tại trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamSession
        {
            Id = Guid.NewGuid(),
            SemesterId = req.SemesterId,
            Code = code,
            Title = title,
            StartsAtUtc = req.StartsAtUtc,
            ExamDurationMinutes = req.ExamDurationMinutes,
            EndsAtUtc = req.EndsAtUtc,
            DeferredClassGrading = req.DeferredClassGrading,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamSessions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await BuildExamSessionListItemAsync(entity.Id, cancellationToken);
        return Result<ExamSessionListItemDto>.Success(dto, "Đã tạo ca thi.");
    }

    public async Task<Result<ExamSessionListItemDto>> UpdateExamSessionAsync(Guid id, UpdateExamSessionRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamSessions.Include(x => x.Semester).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return Result<ExamSessionListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (!await _db.Semesters.AnyAsync(x => x.Id == req.SemesterId, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = req.Code.Trim();
        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(title))
            return Result<ExamSessionListItemDto>.Failure("Code và Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (req.ExamDurationMinutes <= 0)
            return Result<ExamSessionListItemDto>.Failure("examDurationMinutes phải > 0.", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamSessions.AnyAsync(x => x.SemesterId == req.SemesterId && x.Code == code && x.Id != id, cancellationToken))
            return Result<ExamSessionListItemDto>.Failure("Mã ca thi đã được dùng trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        entity.SemesterId = req.SemesterId;
        entity.Code = code;
        entity.Title = title;
        entity.StartsAtUtc = req.StartsAtUtc;
        entity.ExamDurationMinutes = req.ExamDurationMinutes;
        entity.EndsAtUtc = req.EndsAtUtc;
        entity.DeferredClassGrading = req.DeferredClassGrading;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await BuildExamSessionListItemAsync(entity.Id, cancellationToken);
        return Result<ExamSessionListItemDto>.Success(dto, "Đã cập nhật ca thi.");
    }

    public async Task<Result<bool>> DeleteExamSessionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamSessions
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .Include(x => x.GradingPacks).ThenInclude(p => p.Assets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (await _db.ExamSubmissions.AnyAsync(x => x.ExamSessionId == id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — đã có bài nộp.", ErrorCodeEnum.BusinessRuleViolation);

        var now = DateTime.UtcNow;
        foreach (var tc in entity.Topics.SelectMany(t => t.Questions).SelectMany(q => q.TestCases))
        {
            tc.IsDeleted = true;
            tc.DeletedAt = now;
            tc.UpdatedAt = now;
        }

        foreach (var q in entity.Topics.SelectMany(t => t.Questions))
        {
            q.IsDeleted = true;
            q.DeletedAt = now;
            q.UpdatedAt = now;
        }

        foreach (var t in entity.Topics)
        {
            t.IsDeleted = true;
            t.DeletedAt = now;
            t.UpdatedAt = now;
        }

        var fileService = _fileServiceFactory.CreateFileService();
        foreach (var asset in entity.GradingPacks.SelectMany(p => p.Assets))
        {
            try
            {
                await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không xóa được file asset {Path}", asset.StorageRelativePath);
            }

            asset.IsDeleted = true;
            asset.DeletedAt = now;
            asset.UpdatedAt = now;
        }

        foreach (var pack in entity.GradingPacks)
        {
            pack.IsDeleted = true;
            pack.DeletedAt = now;
            pack.UpdatedAt = now;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa ca thi và cấu trúc đề kèm theo.");
    }

    public async Task<Result<ExamTopicDetailDto>> CreateTopicAsync(Guid examSessionId, CreateExamTopicRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken))
            return Result<ExamTopicDetailDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(title))
            return Result<ExamTopicDetailDto>.Failure("Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        var entity = new ExamTopic
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            Title = title,
            SortOrder = req.SortOrder,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamTopics.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamTopicDetailDto>.Success(
            new ExamTopicDetailDto(entity.Id, entity.Title, entity.SortOrder, Array.Empty<ExamQuestionDetailDto>()),
            "Đã tạo chủ đề.");
    }

    public async Task<Result<ExamTopicDetailDto>> UpdateTopicAsync(Guid topicId, UpdateExamTopicRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamTopics.FirstOrDefaultAsync(x => x.Id == topicId, cancellationToken);
        if (entity == null)
            return Result<ExamTopicDetailDto>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(title))
            return Result<ExamTopicDetailDto>.Failure("Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        entity.Title = title;
        entity.SortOrder = req.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var questions = await LoadQuestionsForTopicAsync(topicId, cancellationToken);
        return Result<ExamTopicDetailDto>.Success(
            new ExamTopicDetailDto(entity.Id, entity.Title, entity.SortOrder, questions),
            "Đã cập nhật chủ đề.");
    }

    public async Task<Result<bool>> DeleteTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamTopics
            .Include(x => x.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == topicId, cancellationToken);

        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        var now = DateTime.UtcNow;
        foreach (var tc in entity.Questions.SelectMany(q => q.TestCases))
        {
            tc.IsDeleted = true;
            tc.DeletedAt = now;
            tc.UpdatedAt = now;
        }

        foreach (var q in entity.Questions)
        {
            q.IsDeleted = true;
            q.DeletedAt = now;
            q.UpdatedAt = now;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa chủ đề.");
    }

    public async Task<Result<ExamQuestionDetailDto>> CreateQuestionAsync(Guid topicId, CreateExamQuestionRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamTopics.AnyAsync(x => x.Id == topicId, cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Không tìm thấy chủ đề.", ErrorCodeEnum.NotFound);

        var label = req.Label.Trim();
        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(title))
            return Result<ExamQuestionDetailDto>.Failure("Label và Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamQuestions.AnyAsync(x => x.ExamTopicId == topicId && x.Label.ToLower() == label.ToLower(), cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Label câu hỏi đã tồn tại trong chủ đề.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            ExamTopicId = topicId,
            Label = label,
            Title = title,
            MaxScore = req.MaxScore,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamQuestions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamQuestionDetailDto>.Success(
            new ExamQuestionDetailDto(entity.Id, entity.Label, entity.Title, entity.MaxScore, Array.Empty<ExamTestCaseDetailDto>()),
            "Đã tạo câu hỏi.");
    }

    public async Task<Result<ExamQuestionDetailDto>> UpdateQuestionAsync(Guid questionId, UpdateExamQuestionRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamQuestions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        if (entity == null)
            return Result<ExamQuestionDetailDto>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        var label = req.Label.Trim();
        var title = req.Title.Trim();
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(title))
            return Result<ExamQuestionDetailDto>.Failure("Label và Title bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamQuestions.AnyAsync(x => x.ExamTopicId == entity.ExamTopicId && x.Label.ToLower() == label.ToLower() && x.Id != questionId, cancellationToken))
            return Result<ExamQuestionDetailDto>.Failure("Label đã được dùng trong chủ đề.", ErrorCodeEnum.DuplicateEntry);

        entity.Label = label;
        entity.Title = title;
        entity.MaxScore = req.MaxScore;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var testCases = await LoadTestCasesForQuestionAsync(questionId, cancellationToken);
        return Result<ExamQuestionDetailDto>.Success(
            new ExamQuestionDetailDto(entity.Id, entity.Label, entity.Title, entity.MaxScore, testCases),
            "Đã cập nhật câu hỏi.");
    }

    public async Task<Result<bool>> DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamQuestions.Include(x => x.TestCases).FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        var now = DateTime.UtcNow;
        foreach (var tc in entity.TestCases)
        {
            tc.IsDeleted = true;
            tc.DeletedAt = now;
            tc.UpdatedAt = now;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa câu hỏi.");
    }

    public async Task<Result<ExamTestCaseDetailDto>> CreateTestCaseAsync(Guid questionId, CreateExamTestCaseRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamQuestions.AnyAsync(x => x.Id == questionId, cancellationToken))
            return Result<ExamTestCaseDetailDto>.Failure("Không tìm thấy câu hỏi.", ErrorCodeEnum.NotFound);

        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return Result<ExamTestCaseDetailDto>.Failure("Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        var entity = new ExamTestCase
        {
            Id = Guid.NewGuid(),
            ExamQuestionId = questionId,
            Name = name,
            MaxPoints = req.MaxPoints,
            SortOrder = req.SortOrder,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamTestCases.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamTestCaseDetailDto>.Success(
            new ExamTestCaseDetailDto(entity.Id, entity.Name, entity.MaxPoints, entity.SortOrder),
            "Đã tạo testcase.");
    }

    public async Task<Result<ExamTestCaseDetailDto>> UpdateTestCaseAsync(Guid testCaseId, UpdateExamTestCaseRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamTestCases.FirstOrDefaultAsync(x => x.Id == testCaseId, cancellationToken);
        if (entity == null)
            return Result<ExamTestCaseDetailDto>.Failure("Không tìm thấy testcase.", ErrorCodeEnum.NotFound);

        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return Result<ExamTestCaseDetailDto>.Failure("Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        entity.Name = name;
        entity.MaxPoints = req.MaxPoints;
        entity.SortOrder = req.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamTestCaseDetailDto>.Success(
            new ExamTestCaseDetailDto(entity.Id, entity.Name, entity.MaxPoints, entity.SortOrder),
            "Đã cập nhật testcase.");
    }

    public async Task<Result<bool>> DeleteTestCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamTestCases.FirstOrDefaultAsync(x => x.Id == testCaseId, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy testcase.", ErrorCodeEnum.NotFound);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa testcase.");
    }

    public async Task<Result<IReadOnlyList<ExamTestCaseDetailDto>>> ListTestCasesAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var sessionExists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!sessionExists)
            return Result<IReadOnlyList<ExamTestCaseDetailDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var testCases = await _db.ExamTestCases.AsNoTracking()
            .Where(x => x.ExamQuestion.ExamTopic.ExamSessionId == examSessionId)
            .OrderBy(x => x.ExamQuestion.ExamTopic.SortOrder)
            .ThenBy(x => x.ExamQuestion.Label)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ExamTestCaseDetailDto(x.Id, x.Name, x.MaxPoints, x.SortOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExamTestCaseDetailDto>>.Success(testCases, "OK");
    }

    public async Task<Result<List<ExamGradingPackListItemDto>>> ListGradingPacksAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken))
            return Result<List<ExamGradingPackListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamGradingPacks.AsNoTracking()
            .Where(x => x.ExamSessionId == examSessionId)
            .OrderByDescending(x => x.Version)
            .Select(x => new ExamGradingPackListItemDto(x.Id, x.Version, x.Label, x.IsActive, x.Assets.Count))
            .ToListAsync(cancellationToken);

        return Result<List<ExamGradingPackListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<ExamGradingPackListItemDto>> CreateGradingPackAsync(Guid examSessionId, CreateGradingPackRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken))
            return Result<ExamGradingPackListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var label = req.Label.Trim();
        if (string.IsNullOrEmpty(label))
            return Result<ExamGradingPackListItemDto>.Failure("Label bắt buộc.", ErrorCodeEnum.ValidationFailed);

        int version;
        if (req.Version is null or <= 0)
        {
            var max = await _db.ExamGradingPacks.Where(x => x.ExamSessionId == examSessionId).Select(x => (int?)x.Version).MaxAsync(cancellationToken);
            version = (max ?? 0) + 1;
        }
        else
        {
            version = req.Version.Value;
            if (await _db.ExamGradingPacks.AnyAsync(x => x.ExamSessionId == examSessionId && x.Version == version, cancellationToken))
                return Result<ExamGradingPackListItemDto>.Failure("Version đã tồn tại.", ErrorCodeEnum.DuplicateEntry);
        }

        var entity = new ExamGradingPack
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            Version = version,
            Label = label,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };

        if (req.IsActive)
            await DeactivateOtherPacksAsync(examSessionId, entity.Id, cancellationToken);

        _db.ExamGradingPacks.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamGradingPackListItemDto>.Success(
            new ExamGradingPackListItemDto(entity.Id, entity.Version, entity.Label, entity.IsActive, 0),
            "Đã tạo grading pack.");
    }

    public async Task<Result<IReadOnlyList<RequestItem>>> BuildRequestItemsAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var sessionExists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!sessionExists)
            return Result<IReadOnlyList<RequestItem>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamTestCases.AsNoTracking()
            .Where(x => x.ExamQuestion.ExamTopic.ExamSessionId == examSessionId)
            .OrderBy(x => x.ExamQuestion.ExamTopic.SortOrder)
            .ThenBy(x => x.ExamQuestion.Label)
            .ThenBy(x => x.SortOrder)
            .Select(x => new RequestItem(
                x.Method,
                "{{baseUrl}}" + x.UrlTemplate,
                string.IsNullOrWhiteSpace(x.RequestBody) ? null : x.RequestBody))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RequestItem>>.Success(rows, "OK");
    }

    public async Task<Result<IReadOnlyList<string>>> BuildTestScriptsAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var sessionExists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!sessionExists)
            return Result<IReadOnlyList<string>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var testCases = await _db.ExamTestCases.AsNoTracking()
            .Where(x => x.ExamQuestion.ExamTopic.ExamSessionId == examSessionId)
            .OrderBy(x => x.ExamQuestion.ExamTopic.SortOrder)
            .ThenBy(x => x.ExamQuestion.Label)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var scripts = new List<string>();
        foreach (var testCase in testCases)
        {
            scripts.AddRange(BuildTestCaseScriptLines(testCase));
            scripts.Add(string.Empty);
        }

        return Result<IReadOnlyList<string>>.Success(scripts, "OK");
    }

    public async Task<Result<string>> BuildCollectionJsonAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.AsNoTracking()
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == examSessionId, cancellationToken);

        if (session == null)
            return Result<string>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var testCases = await _db.ExamTestCases.AsNoTracking()
            .Where(x => x.ExamQuestion.ExamTopic.ExamSessionId == examSessionId)
            .OrderBy(x => x.ExamQuestion.ExamTopic.SortOrder)
            .ThenBy(x => x.ExamQuestion.Label)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var items = new List<object>();
        foreach (var testCase in testCases)
        {
            items.Add(BuildCollectionItem(testCase));
        }

        var collection = new
        {
            info = new
            {
                name = $"{session.Code} - {session.Title}",
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            variable = new[]
            {
                new { key = "baseUrl", value = "http://localhost:5000" },
                new { key = "examSessionId", value = examSessionId.ToString() },
                new { key = "semesterCode", value = session.Semester.Code }
            },
            item = items
        };

        var json = System.Text.Json.JsonSerializer.Serialize(collection, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Result<string>.Success(json, "OK");
    }

    public async Task<Result<ExamGradingPackListItemDto>> UpdateGradingPackAsync(Guid packId, UpdateGradingPackRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamGradingPacks.Include(x => x.Assets).FirstOrDefaultAsync(x => x.Id == packId, cancellationToken);
        if (entity == null)
            return Result<ExamGradingPackListItemDto>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        var label = req.Label.Trim();
        if (string.IsNullOrEmpty(label))
            return Result<ExamGradingPackListItemDto>.Failure("Label bắt buộc.", ErrorCodeEnum.ValidationFailed);

        entity.Label = label;
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (req.IsActive)
            await DeactivateOtherPacksAsync(entity.ExamSessionId, entity.Id, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamGradingPackListItemDto>.Success(
            new ExamGradingPackListItemDto(entity.Id, entity.Version, entity.Label, entity.IsActive, entity.Assets.Count),
            "Đã cập nhật pack.");
    }

    public async Task<Result<bool>> DeleteGradingPackAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamGradingPacks.Include(x => x.Assets).FirstOrDefaultAsync(x => x.Id == packId, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        var fileService = _fileServiceFactory.CreateFileService();
        var now = DateTime.UtcNow;
        foreach (var asset in entity.Assets)
        {
            try
            {
                await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không xóa file pack asset {Path}", asset.StorageRelativePath);
            }

            asset.IsDeleted = true;
            asset.DeletedAt = now;
            asset.UpdatedAt = now;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa pack.");
    }

    public async Task<Result<ExamPackAssetListItemDto>> CreatePackAssetAsync(
        Guid packId,
        ExamPackAssetKind kind,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var pack = await _db.ExamGradingPacks.FirstOrDefaultAsync(x => x.Id == packId, cancellationToken);
        if (pack == null)
            return Result<ExamPackAssetListItemDto>.Failure("Không tìm thấy pack.", ErrorCodeEnum.NotFound);

        if (file == null || file.Length == 0)
            return Result<ExamPackAssetListItemDto>.Failure("Thiếu file.", ErrorCodeEnum.ValidationFailed);

        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrEmpty(safeName))
            safeName = "asset.bin";

        var fileService = _fileServiceFactory.CreateFileService();
        var subDir = Path.Combine("exam-pack-assets", packId.ToString("N"));
        var storedName = $"{Guid.NewGuid():N}_{safeName}";
        string relativePath;
        try
        {
            relativePath = await fileService.UploadFileAsync(file, storedName, subDir, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload pack asset thất bại");
            return Result<ExamPackAssetListItemDto>.Failure("Upload thất bại.", ErrorCodeEnum.FileUploadFailed);
        }

        var asset = new ExamPackAsset
        {
            Id = Guid.NewGuid(),
            ExamGradingPackId = packId,
            Kind = kind,
            StorageRelativePath = relativePath,
            OriginalFileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamPackAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ExamPackAssetListItemDto>.Success(
            new ExamPackAssetListItemDto(asset.Id, asset.ExamGradingPackId, (int)asset.Kind, asset.StorageRelativePath, asset.OriginalFileName),
            "Đã thêm asset.");
    }

    public async Task<Result<bool>> DeletePackAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await _db.ExamPackAssets.FirstOrDefaultAsync(x => x.Id == assetId, cancellationToken);
        if (asset == null)
            return Result<bool>.Failure("Không tìm thấy asset.", ErrorCodeEnum.NotFound);

        var fileService = _fileServiceFactory.CreateFileService();
        try
        {
            await fileService.DeleteFileAsync(asset.StorageRelativePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không xóa file asset {Path}", asset.StorageRelativePath);
        }

        asset.IsDeleted = true;
        asset.DeletedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa asset.");
    }

    public async Task<Result<List<ExamClassListItemDto>>> ListExamClassesAsync(Guid semesterId, CancellationToken cancellationToken = default)
    {
        if (!await _db.Semesters.AnyAsync(x => x.Id == semesterId, cancellationToken))
            return Result<List<ExamClassListItemDto>>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamClasses.AsNoTracking()
            .Where(x => x.SemesterId == semesterId)
            .OrderBy(x => x.Code)
            .Select(x => new ExamClassListItemDto(x.Id, x.SemesterId, x.Code, x.Name, x.MaxStudents))
            .ToListAsync(cancellationToken);
        return Result<List<ExamClassListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<ExamClassListItemDto>> CreateExamClassAsync(Guid semesterId, CreateExamClassRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Semesters.AnyAsync(x => x.Id == semesterId, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Không tìm thấy học kỳ.", ErrorCodeEnum.NotFound);

        var code = req.Code.Trim();
        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Result<ExamClassListItemDto>.Failure("Code và Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        var max = req.MaxStudents <= 0 ? 35 : req.MaxStudents;
        if (max > 200)
            return Result<ExamClassListItemDto>.Failure("MaxStudents quá lớn (tối đa 200).", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamClasses.AnyAsync(x => x.SemesterId == semesterId && x.Code == code, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Mã lớp đã tồn tại trong học kỳ.", ErrorCodeEnum.DuplicateEntry);

        var entity = new ExamClass
        {
            Id = Guid.NewGuid(),
            SemesterId = semesterId,
            Code = code,
            Name = name,
            MaxStudents = max,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamClasses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamClassListItemDto>.Success(
            new ExamClassListItemDto(entity.Id, entity.SemesterId, entity.Code, entity.Name, entity.MaxStudents),
            "Đã tạo lớp.");
    }

    public async Task<Result<ExamClassListItemDto>> UpdateExamClassAsync(Guid id, UpdateExamClassRequest req, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamClasses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return Result<ExamClassListItemDto>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);

        var code = req.Code.Trim();
        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Result<ExamClassListItemDto>.Failure("Code và Name bắt buộc.", ErrorCodeEnum.ValidationFailed);

        if (req.MaxStudents <= 0)
            return Result<ExamClassListItemDto>.Failure("MaxStudents phải > 0.", ErrorCodeEnum.ValidationFailed);
        if (req.MaxStudents > 200)
            return Result<ExamClassListItemDto>.Failure("MaxStudents quá lớn (tối đa 200).", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamClasses.AnyAsync(x => x.SemesterId == entity.SemesterId && x.Code == code && x.Id != id, cancellationToken))
            return Result<ExamClassListItemDto>.Failure("Mã lớp đã được dùng.", ErrorCodeEnum.DuplicateEntry);

        var usedExpected = await _db.ExamSessionClasses
            .Where(x => x.ExamClassId == id && x.ExpectedStudentCount > req.MaxStudents)
            .AnyAsync(cancellationToken);
        if (usedExpected)
            return Result<ExamClassListItemDto>.Failure(
                "Không giảm MaxStudents xuống thấp hơn ExpectedStudentCount của một ca đã gắn lớp.",
                ErrorCodeEnum.BusinessRuleViolation);

        entity.Code = code;
        entity.Name = name;
        entity.MaxStudents = req.MaxStudents;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExamClassListItemDto>.Success(
            new ExamClassListItemDto(entity.Id, entity.SemesterId, entity.Code, entity.Name, entity.MaxStudents),
            "Đã cập nhật lớp.");
    }

    public async Task<Result<bool>> DeleteExamClassAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamClasses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);

        if (await _db.ExamSessionClasses.AnyAsync(x => x.ExamClassId == id, cancellationToken))
            return Result<bool>.Failure("Không xóa được — lớp đã gắn vào ca thi.", ErrorCodeEnum.BusinessRuleViolation);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã xóa lớp.");
    }

    public async Task<Result<List<ExamSessionClassListItemDto>>> ListExamSessionClassesAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken))
            return Result<List<ExamSessionClassListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamSessionClasses.AsNoTracking()
            .Where(x => x.ExamSessionId == examSessionId)
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
        var subs = await _db.ExamSubmissions.AsNoTracking()
            .Where(s => s.ExamSessionClassId != null && classIds.Contains(s.ExamSessionClassId.Value))
            .Include(s => s.SubmissionFiles)
            .ToListAsync(cancellationToken);

        static bool HasQ1Q2(ICollection<ExamSubmissionFile> files) =>
            files.Any(f => f.QuestionLabel.Equals("Q1", StringComparison.OrdinalIgnoreCase)) &&
            files.Any(f => f.QuestionLabel.Equals("Q2", StringComparison.OrdinalIgnoreCase));

        var byClass = subs.GroupBy(s => s.ExamSessionClassId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var dtos = rows.Select(x =>
        {
            var list = byClass.GetValueOrDefault(x.Id) ?? new List<ExamSubmission>();
            var total = list.Count;
            var ready = list.Count(s =>
                HasQ1Q2(s.SubmissionFiles) &&
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

    public async Task<Result<ExamSessionClassListItemDto>> CreateExamSessionClassAsync(
        Guid examSessionId,
        CreateExamSessionClassRequest req,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examSessionId, cancellationToken);
        if (session == null)
            return Result<ExamSessionClassListItemDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var examClass = await _db.ExamClasses.FirstOrDefaultAsync(x => x.Id == req.ExamClassId, cancellationToken);
        if (examClass == null)
            return Result<ExamSessionClassListItemDto>.Failure("Không tìm thấy lớp.", ErrorCodeEnum.NotFound);

        if (examClass.SemesterId != session.SemesterId)
            return Result<ExamSessionClassListItemDto>.Failure("Lớp không cùng học kỳ với ca thi.", ErrorCodeEnum.ValidationFailed);

        if (await _db.ExamSessionClasses.AnyAsync(x => x.ExamSessionId == examSessionId && x.ExamClassId == req.ExamClassId, cancellationToken))
            return Result<ExamSessionClassListItemDto>.Failure("Ca thi đã gắn lớp này rồi.", ErrorCodeEnum.DuplicateEntry);

        var expected = req.ExpectedStudentCount <= 0 ? examClass.MaxStudents : req.ExpectedStudentCount;
        if (expected > examClass.MaxStudents)
            return Result<ExamSessionClassListItemDto>.Failure(
                $"ExpectedStudentCount không được vượt MaxStudents của lớp ({examClass.MaxStudents}).",
                ErrorCodeEnum.ValidationFailed);

        var entity = new ExamSessionClass
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            ExamClassId = req.ExamClassId,
            ExpectedStudentCount = expected,
            BatchStatus = ClassGradingBatchStatus.CollectingSubmissions,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.ExamSessionClasses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = (await ListExamSessionClassesAsync(examSessionId, cancellationToken)).Data!
            .First(x => x.Id == entity.Id);
        return Result<ExamSessionClassListItemDto>.Success(dto, "Đã gắn lớp vào ca thi.");
    }

    public async Task<Result<bool>> DeleteExamSessionClassAsync(Guid examSessionClassId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExamSessionClasses.FirstOrDefaultAsync(x => x.Id == examSessionClassId, cancellationToken);
        if (entity == null)
            return Result<bool>.Failure("Không tìm thấy bản ghi lớp trong ca.", ErrorCodeEnum.NotFound);

        if (await _db.ExamSubmissions.AnyAsync(x => x.ExamSessionClassId == examSessionClassId, cancellationToken))
            return Result<bool>.Failure("Không xóa được — đã có bài nộp cho lớp trong ca này.", ErrorCodeEnum.BusinessRuleViolation);

        // Junction: xóa hẳn để có thể gắn lại (unique session+class).
        _db.ExamSessionClasses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true, "Đã gỡ lớp khỏi ca thi.");
    }

    private async Task DeactivateOtherPacksAsync(Guid examSessionId, Guid exceptPackId, CancellationToken cancellationToken)
    {
        var others = await _db.ExamGradingPacks
            .Where(x => x.ExamSessionId == examSessionId && x.Id != exceptPackId && x.IsActive)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var p in others)
        {
            p.IsActive = false;
            p.UpdatedAt = now;
        }
    }

    private async Task<ExamSessionListItemDto> BuildExamSessionListItemAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.ExamSessions.AsNoTracking()
            .Where(x => x.Id == sessionId)
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
    }

    private async Task<IReadOnlyList<ExamQuestionDetailDto>> LoadQuestionsForTopicAsync(Guid topicId, CancellationToken cancellationToken)
    {
        return await _db.ExamQuestions.AsNoTracking()
            .Where(x => x.ExamTopicId == topicId)
            .OrderBy(x => x.Label)
            .Select(x => new ExamQuestionDetailDto(
                x.Id,
                x.Label,
                x.Title,
                x.MaxScore,
                x.TestCases.OrderBy(tc => tc.SortOrder).Select(tc => new ExamTestCaseDetailDto(tc.Id, tc.Name, tc.MaxPoints, tc.SortOrder)).ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ExamTestCaseDetailDto>> LoadTestCasesForQuestionAsync(Guid questionId, CancellationToken cancellationToken)
    {
        return await _db.ExamTestCases.AsNoTracking()
            .Where(x => x.ExamQuestionId == questionId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ExamTestCaseDetailDto(x.Id, x.Name, x.MaxPoints, x.SortOrder))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<string> BuildTestCaseScriptLines(ExamTestCase testCase)
    {
        var title = EscapeJsString($"{testCase.ExamQuestion.Label} - {testCase.Name}");
        var scoreText = testCase.MaxPoints.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            $"// {testCase.ExamQuestion.Label} - {testCase.Name}",
            $"pm.test(\"{title} - status {testCase.ExpectedStatus}\", function () {{",
            $"  pm.response.to.have.status({testCase.ExpectedStatus});",
            "});"
        };

        var expectedBody = (testCase.ExpectedBody ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(expectedBody))
        {
            lines.Add($"pm.test(\"{title} - body validation\", function () {{");
            lines.Add("  const bodyText = pm.response.text();");

            if (expectedBody.Equals("empty_array", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add("  const body = pm.response.json();");
                lines.Add("  pm.expect(Array.isArray(body)).to.be.true;");
                lines.Add("  pm.expect(body.length).to.equal(0);");
            }
            else if (expectedBody.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = EscapeJsRegex(expectedBody[6..].Trim());
                lines.Add($"  pm.expect(bodyText).to.match(/{pattern}/);");
            }
            else if (expectedBody.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
            {
                var expectedText = EscapeJsString(expectedBody[9..].Trim());
                lines.Add($"  pm.expect(bodyText).to.include(\"{expectedText}\");");
            }
            else
            {
                var expectedText = EscapeJsString(expectedBody);
                lines.Add($"  pm.expect(bodyText).to.include(\"{expectedText}\");");
            }

            lines.Add("});");
        }

        lines.Add($"pm.test(\"{title} - score {scoreText}\", function () {{");
        lines.Add($"  pm.environment.set(\"score_{testCase.Id:N}\", \"{scoreText}\");");
        lines.Add("});");

        return lines;
    }

    private static object BuildCollectionItem(ExamTestCase testCase)
    {
        var scriptLines = BuildTestCaseScriptLines(testCase);
        var body = string.IsNullOrWhiteSpace(testCase.RequestBody)
            ? null
            : new
            {
                mode = "raw",
                raw = testCase.RequestBody,
                options = new { raw = new { language = "json" } }
            };

        return new
        {
            name = $"{testCase.ExamQuestion.Label} - {testCase.Name}",
            request = new
            {
                method = string.IsNullOrWhiteSpace(testCase.Method) ? "GET" : testCase.Method.ToUpperInvariant(),
                header = Array.Empty<object>(),
                url = new
                {
                    raw = "{{baseUrl}}" + testCase.UrlTemplate,
                    host = new[] { "{{baseUrl}}" },
                    path = NormalizeCollectionPath(testCase.UrlTemplate)
                },
                body
            },
            @event = new[]
            {
                new
                {
                    listen = "test",
                    script = new
                    {
                        type = "text/javascript",
                        exec = scriptLines
                    }
                }
            }
        };
    }

    private static string[] NormalizeCollectionPath(string urlTemplate)
    {
        return urlTemplate
            .Trim()
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string EscapeJsString(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", string.Empty)
            .Replace("\n", "\\n");

    private static string EscapeJsRegex(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("/", "\\/")
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
}
