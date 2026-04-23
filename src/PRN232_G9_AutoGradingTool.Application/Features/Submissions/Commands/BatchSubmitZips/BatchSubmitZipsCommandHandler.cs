using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;

public class BatchSubmitZipsCommandHandler
    : IRequestHandler<BatchSubmitZipsCommand, Result<BatchSubmitZipsResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<BatchSubmitZipsCommandHandler> _logger;

    public BatchSubmitZipsCommandHandler(
        IUnitOfWork unitOfWork,
        IFileServiceFactory fileServiceFactory,
        IMapper mapper,
        ILogger<BatchSubmitZipsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileServiceFactory = fileServiceFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BatchSubmitZipsResponseDto>> Handle(
        BatchSubmitZipsCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load session with topics
        var session = await _unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .Include(s => s.Topics.OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == command.ExamSessionId, cancellationToken);

        if (session is null)
            return Result<BatchSubmitZipsResponseDto>.Failure(
                "Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        // 2. Window check
        if (!command.BypassExamWindow)
        {
            var now = DateTime.UtcNow;
            if (now < session.StartsAtUtc)
                return Result<BatchSubmitZipsResponseDto>.Failure(
                    "Ca thi chưa bắt đầu.", ErrorCodeEnum.BusinessRuleViolation);
            if (now > session.EndsAtUtc)
                return Result<BatchSubmitZipsResponseDto>.Failure(
                    "Ca thi đã kết thúc.", ErrorCodeEnum.BusinessRuleViolation);
        }

        // 3. Topics guard
        if (!session.Topics.Any())
            return Result<BatchSubmitZipsResponseDto>.Failure(
                "Ca thi chưa có topic nào.", ErrorCodeEnum.BusinessRuleViolation);

        var topicsById = session.Topics.ToDictionary(t => t.Id);
        var fileService = _fileServiceFactory.CreateFileService();
        var results = new List<StudentSubmitResultItem>();

        // 4. Per-student loop — each student saved independently to isolate failures
        foreach (var entry in command.Request.Entries)
        {
            var code = entry.StudentCode.Trim();
            try
            {
                if (!topicsById.TryGetValue(entry.ExamTopicId, out var topic))
                {
                    results.Add(new StudentSubmitResultItem(
                        code,
                        false,
                        null,
                        "ExamTopicId khÃ´ng thuá»™c ca thi."));
                    continue;
                }

                // a. Duplicate check
                var isDuplicate = await _unitOfWork.Repository<ExamSubmission>()
                    .AnyAsync(x => x.ExamSessionId == command.ExamSessionId
                                && x.StudentCode == code, cancellationToken);

                if (isDuplicate)
                {
                    results.Add(new StudentSubmitResultItem(code, false, null,
                        "Sinh viên đã nộp bài trong ca thi này."));
                    continue;
                }

                // b. Load active grading pack (nullable — upload still proceeds)
                var pack = await _unitOfWork.Repository<ExamGradingPack>()
                    .GetQueryable()
                    .Where(p => p.ExamSessionId == command.ExamSessionId && p.IsActive)
                    .OrderByDescending(p => p.Version)
                    .FirstOrDefaultAsync(cancellationToken);

                var submittedAt = DateTime.UtcNow;

                // c. Build submission via mapping
                var submission = _mapper.Map<ExamSubmission>(entry);
                submission.ExamSessionId = session.Id;
                submission.ExamGradingPackId = pack?.Id;
                submission.SubmittedAtUtc = submittedAt;
                submission.InitializeEntity();

                // d. TestResult seed via mapping
                submission.Result = _mapper.Map<TestResult>(submission);
                submission.Result.InitializeEntity();

                // e. Upload files first (before DB commit)
                var baseDir = BuildSubmissionBaseDirectory(session.Code, topic.Id, code, entry.StudentName);

                var p1 = await fileService.UploadFileAsync(
                    entry.Q1Zip, "solution.zip", $"{baseDir}/Q1", cancellationToken);
                var p2 = await fileService.UploadFileAsync(
                    entry.Q2Zip, "solution.zip", $"{baseDir}/Q2", cancellationToken);

                // f. Submission files
                var submissionFiles = new[]
                {
                    BuildSubmissionFile(submission.Id, "Q1", p1, entry.Q1Zip.FileName),
                    BuildSubmissionFile(submission.Id, "Q2", p2, entry.Q2Zip.FileName)
                };

                // g. Persist
                await _unitOfWork.Repository<ExamSubmission>().AddAsync(submission, cancellationToken);
                await _unitOfWork.Repository<ExamSubmissionFile>().AddRangeAsync(submissionFiles, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                results.Add(new StudentSubmitResultItem(code, true, submission.Id, null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to submit for student {StudentCode} in session {SessionId}",
                    code, command.ExamSessionId);
                results.Add(new StudentSubmitResultItem(code, false, null, ex.Message));
            }
        }

        var successCount = results.Count(r => r.Success);
        var dto = new BatchSubmitZipsResponseDto(
            successCount,
            results.Count - successCount,
            results);

        return Result<BatchSubmitZipsResponseDto>.Success(
            dto, $"{successCount}/{results.Count} bài nộp thành công.");
    }

    private static string BuildStudentFolderName(string studentCode, string? studentName)
    {
        var safeName = string.IsNullOrWhiteSpace(studentName)
            ? studentCode
            : $"{studentCode}_{studentName}";

        return string.Concat(safeName.Split(Path.GetInvalidPathChars()));
    }

    internal static string BuildSubmissionBaseDirectory(
        string sessionCode,
        Guid topicId,
        string studentCode,
        string? studentName)
    {
        var folderName = BuildStudentFolderName(studentCode, studentName);
        return $"{sessionCode}/{topicId:N}/{folderName}";
    }

    private static ExamSubmissionFile BuildSubmissionFile(
        Guid submissionId, string label, string storagePath, string originalFileName)
    {
        var file = new ExamSubmissionFile
        {
            ExamSubmissionId = submissionId,
            QuestionLabel = label,
            StorageRelativePath = storagePath,
            OriginalFileName = originalFileName
        };
        file.InitializeEntity();
        return file;
    }
}
