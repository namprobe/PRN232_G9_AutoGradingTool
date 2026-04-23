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

public class ExamGradingAppService : IExamGradingAppService
{
    private readonly PRN232_G9_AutoGradingToolDbContext _db;
    private readonly ILogger<ExamGradingAppService> _logger;

    public ExamGradingAppService(
        PRN232_G9_AutoGradingToolDbContext db,
        ILogger<ExamGradingAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<SemesterListItemDto>>> ListSemestersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Semesters.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new SemesterListItemDto(x.Id, x.Code, x.Name, x.StartDateUtc, x.EndDateUtc))
            .ToListAsync(cancellationToken);
        return Result<List<SemesterListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<List<ExamSessionListItemDto>>> ListExamSessionsAsync(Guid? semesterId, CancellationToken cancellationToken = default)
    {
        var q = _db.ExamSessions.AsNoTracking().Include(x => x.Semester).AsQueryable();
        if (semesterId.HasValue)
            q = q.Where(x => x.SemesterId == semesterId.Value);

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

    public async Task<Result<ExamSessionDetailDto>> GetExamSessionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.AsNoTracking()
            .Include(x => x.Semester)
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session == null)
            return Result<ExamSessionDetailDto>.Failure("KhÃ´ng tÃ¬m tháº¥y ca thi.", ErrorCodeEnum.NotFound);

        var topics = session.Topics.OrderBy(t => t.SortOrder).Select(t => new ExamTopicDetailDto(
            t.Id,
            t.Title,
            t.SortOrder,
            t.Questions.OrderBy(q => q.Label).Select(q => new ExamQuestionDetailDto(
                q.Id,
                q.Label,
                q.Title,
                q.MaxScore,
                q.TestCases.OrderBy(tc => tc.SortOrder).Select(tc => new ExamTestCaseDetailDto(tc.Id, tc.Name, tc.MaxPoints, tc.SortOrder)).ToList()
            )).ToList()
        )).ToList();

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

    public async Task<Result<List<ExamSubmissionListItemDto>>> ListSubmissionsAsync(
        Guid examSessionId,
        Guid? examSessionClassId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!exists)
            return Result<List<ExamSubmissionListItemDto>>.Failure("KhÃ´ng tÃ¬m tháº¥y ca thi.", ErrorCodeEnum.NotFound);

        var q = _db.ExamSubmissions.AsNoTracking()
            .Where(x => x.ExamSessionId == examSessionId);
        if (examSessionClassId.HasValue)
            q = q.Where(x => x.ExamSessionClassId == examSessionClassId.Value);

        var rows = await q
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

    public async Task<Result<ExamSubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = await _db.ExamSubmissions.AsNoTracking()
            .Include(x => x.ExamSession)
            .Include(x => x.ExamSessionClass).ThenInclude(c => c!.ExamClass)
            .Include(x => x.SubmissionFiles)
            .Include(x => x.QuestionScores).ThenInclude(qs => qs.ExamQuestion)
            .Include(x => x.Result).ThenInclude(r => r!.Details).ThenInclude(d => d.TestCase)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sub == null)
            return Result<ExamSubmissionDetailDto>.Failure("KhÃ´ng tÃ¬m tháº¥y bÃ i ná»™p.", ErrorCodeEnum.NotFound);

        var qScores = sub.QuestionScores
            .OrderBy(x => x.ExamQuestion.Label)
            .Select(x => new ExamQuestionScoreDto(x.ExamQuestionId, x.ExamQuestion.Label, x.Score, x.MaxScore, x.Summary))
            .ToList();

        var resultDetails = sub.Result?.Details
            .OrderBy(x => x.TestCase.SortOrder)
            .Select(x => new ResultDetail(
                x.TestCase.Name,
                x.Passed,
                x.Score,
                x.ResponseTime,
                x.ErrorMessage,
                null))
            .ToList();

        var files = sub.SubmissionFiles
            .OrderBy(x => x.QuestionLabel)
            .Select(x => new SubmissionFileDto(x.QuestionLabel, x.StorageRelativePath, x.OriginalFileName))
            .ToList();

        var dto = new ExamSubmissionDetailDto(
            sub.Id,
            sub.ExamSessionId,
            sub.ExamSession.Code,
            sub.ExamSessionClassId,
            sub.ExamSessionClass?.ExamClass.Code,
            sub.StudentCode,
            sub.StudentName,
            sub.WorkflowStatus.ToString(),
            sub.SubmittedAtUtc,
            sub.TotalScore,
            files,
            qScores,
            resultDetails ?? new List<ResultDetail>());

        return Result<ExamSubmissionDetailDto>.Success(dto, "OK");
    }

    public Task<Result<Guid>> CreateSubmissionWithZipAsync(Guid examSessionId, string studentCode, string? studentName, IFormFile q1Zip, IFormFile q2Zip, bool bypassExamWindow, Guid? examSessionClassId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<StartClassBatchGradingResponseDto>> StartClassBatchGradingAsync(Guid id, StartClassBatchGradingRequest body, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> ReplaceSubmissionFileAsync(Guid id, string questionLabel, IFormFile zipFile, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

