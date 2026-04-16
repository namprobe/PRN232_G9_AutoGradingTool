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
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly ILogger<ExamGradingAppService> _logger;

    public ExamGradingAppService(
        PRN232_G9_AutoGradingToolDbContext db,
        IFileServiceFactory fileServiceFactory,
        ILogger<ExamGradingAppService> logger)
    {
        _db = db;
        _fileServiceFactory = fileServiceFactory;
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
            .OrderByDescending(x => x.ScheduledAtUtc)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.ScheduledAtUtc,
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
            return Result<ExamSessionDetailDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

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
            session.ScheduledAtUtc,
            topics);

        return Result<ExamSessionDetailDto>.Success(dto, "OK");
    }

    public async Task<Result<List<ExamSubmissionListItemDto>>> ListSubmissionsAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!exists)
            return Result<List<ExamSubmissionListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamSubmissions.AsNoTracking()
            .Where(x => x.ExamSessionId == examSessionId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new ExamSubmissionListItemDto(
                x.Id,
                x.ExamSessionId,
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
            .Include(x => x.QuestionScores).ThenInclude(qs => qs.ExamQuestion)
            .Include(x => x.TestCaseScores).ThenInclude(ts => ts.ExamTestCase).ThenInclude(tc => tc!.ExamQuestion)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sub == null)
            return Result<ExamSubmissionDetailDto>.Failure("Không tìm thấy bài nộp.", ErrorCodeEnum.NotFound);

        var qScores = sub.QuestionScores
            .OrderBy(x => x.ExamQuestion.Label)
            .Select(x => new ExamQuestionScoreDto(x.ExamQuestionId, x.ExamQuestion.Label, x.Score, x.MaxScore, x.Summary))
            .ToList();

        var tcScores = sub.TestCaseScores
            .OrderBy(x => x.ExamTestCase.ExamQuestion.Label).ThenBy(x => x.ExamTestCase.SortOrder)
            .Select(x => new ExamTestCaseScoreDto(
                x.ExamTestCaseId,
                x.ExamTestCase.ExamQuestion.Label,
                x.ExamTestCase.Name,
                x.PointsEarned,
                x.MaxPoints,
                x.Outcome.ToString(),
                x.Message))
            .ToList();

        var dto = new ExamSubmissionDetailDto(
            sub.Id,
            sub.ExamSessionId,
            sub.ExamSession.Code,
            sub.StudentCode,
            sub.StudentName,
            sub.WorkflowStatus.ToString(),
            sub.SubmittedAtUtc,
            sub.TotalScore,
            sub.Q1ZipRelativePath,
            sub.Q2ZipRelativePath,
            qScores,
            tcScores);

        return Result<ExamSubmissionDetailDto>.Success(dto, "OK");
    }

    public async Task<Result<Guid>> CreateSubmissionWithZipAsync(
        Guid examSessionId,
        string studentCode,
        string? studentName,
        IFormFile q1Zip,
        IFormFile q2Zip,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentCode))
            return Result<Guid>.Failure("MSSV / studentCode bắt buộc.", ErrorCodeEnum.ValidationFailed);

        var session = await _db.ExamSessions
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == examSessionId, cancellationToken);

        if (session == null)
            return Result<Guid>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (!IsZip(q1Zip) || !IsZip(q2Zip))
            return Result<Guid>.Failure("Hai file phải là .zip.", ErrorCodeEnum.InvalidFileType);

        var submission = new ExamSubmission
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            StudentCode = studentCode.Trim(),
            StudentName = string.IsNullOrWhiteSpace(studentName) ? null : studentName.Trim(),
            WorkflowStatus = ExamSubmissionStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };

        _db.ExamSubmissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        var fileService = _fileServiceFactory.CreateFileService();
        var subDir = Path.Combine("exam-submissions", submission.Id.ToString("N"));
        try
        {
            var p1 = await fileService.UploadFileAsync(q1Zip, "q1.zip", subDir, cancellationToken);
            var p2 = await fileService.UploadFileAsync(q2Zip, "q2.zip", subDir, cancellationToken);
            if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2))
                throw new InvalidOperationException("Upload trả về đường dẫn rỗng.");

            submission.Q1ZipRelativePath = p1;
            submission.Q2ZipRelativePath = p2;
            submission.WorkflowStatus = ExamSubmissionStatus.Running;
            await _db.SaveChangesAsync(cancellationToken);

            await ApplyStubGradingAsync(submission.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi upload/chấm stub cho submission {Id}", submission.Id);
            submission.WorkflowStatus = ExamSubmissionStatus.Failed;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Failure("Lưu file hoặc chấm stub thất bại.", ErrorCodeEnum.FileUploadFailed);
        }

        return Result<Guid>.Success(submission.Id, "Đã nhận zip và chạy chấm stub (demo).");
    }

    private static bool IsZip(IFormFile f)
    {
        var n = f.FileName?.ToLowerInvariant() ?? "";
        return n.EndsWith(".zip", StringComparison.Ordinal);
    }

    /// <summary>Stub P3: điểm ổn định để demo — sau thay bằng Hangfire + grader thật.</summary>
    private async Task ApplyStubGradingAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var sub = await _db.ExamSubmissions
            .Include(x => x.ExamSession).ThenInclude(es => es.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .Include(x => x.QuestionScores)
            .Include(x => x.TestCaseScores)
            .FirstAsync(x => x.Id == submissionId, cancellationToken);

        _db.ExamQuestionScores.RemoveRange(sub.QuestionScores);
        _db.ExamTestCaseScores.RemoveRange(sub.TestCaseScores);

        decimal total = 0;
        var now = DateTime.UtcNow;

        foreach (var topic in sub.ExamSession.Topics)
        {
            foreach (var question in topic.Questions.OrderBy(q => q.Label))
            {
                decimal qSum = 0;
                foreach (var tc in question.TestCases.OrderBy(t => t.SortOrder))
                {
                    var earned = Math.Round(tc.MaxPoints * 0.85m, 2, MidpointRounding.AwayFromZero);
                    if (earned > tc.MaxPoints)
                        earned = tc.MaxPoints;
                    qSum += earned;
                    total += earned;

                    _db.ExamTestCaseScores.Add(new ExamTestCaseScore
                    {
                        Id = Guid.NewGuid(),
                        ExamSubmissionId = sub.Id,
                        ExamTestCaseId = tc.Id,
                        PointsEarned = earned,
                        MaxPoints = tc.MaxPoints,
                        Outcome = ExamTestCaseOutcome.Pass,
                        Message = "Stub grader (demo PRN232)",
                        CreatedAt = now,
                        Status = EntityStatusEnum.Active
                    });
                }

                var qMax = question.MaxScore;
                if (qSum > qMax)
                    qSum = qMax;

                _db.ExamQuestionScores.Add(new ExamQuestionScore
                {
                    Id = Guid.NewGuid(),
                    ExamSubmissionId = sub.Id,
                    ExamQuestionId = question.Id,
                    Score = qSum,
                    MaxScore = qMax,
                    Summary = "Tổng testcase (stub)",
                    CreatedAt = now,
                    Status = EntityStatusEnum.Active
                });
            }
        }

        var sessionMax = sub.ExamSession.Topics.SelectMany(t => t.Questions).Sum(q => q.MaxScore);
        if (total > sessionMax)
            total = sessionMax;

        sub.TotalScore = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        sub.WorkflowStatus = ExamSubmissionStatus.Completed;
        sub.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
