using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Mappings;
using PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;
using PRN232_G9_AutoGradingTool.Infrastructure.Repositories;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using Xunit;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Tests;

public class BatchSubmitZipsCommandHandlerTests
{
    [Fact]
    public async Task Handle_UsesTopicIdInUploadedPathsAndPersistsSubmissionFiles()
    {
        await using var db = CreateDbContext(nameof(Handle_UsesTopicIdInUploadedPathsAndPersistsSubmissionFiles));
        var session = CreateSession("SESSION-A");
        var topicA = CreateTopic(session.Id, "Topic A", 1);
        var topicB = CreateTopic(session.Id, "Topic B", 2);

        db.ExamSessions.Add(session);
        db.ExamTopics.AddRange(topicA, topicB);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out var fileService);
        var command = new BatchSubmitZipsCommand(
            session.Id,
            new BatchSubmitZipsRequest
            {
                Entries =
                [
                    new StudentZipEntry
                    {
                        ExamTopicId = topicB.Id,
                        StudentCode = "HE186501",
                        StudentName = "Alice",
                        Q1Zip = CreateZipFormFile("q1.zip"),
                        Q2Zip = CreateZipFormFile("q2.zip")
                    }
                ]
            },
            BypassExamWindow: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data!.SuccessCount);
        Assert.Equal(2, fileService.Uploads.Count);
        Assert.All(fileService.Uploads, upload => Assert.Contains($"{session.Code}/{topicB.Id:N}/HE186501_Alice", upload.SubDirectory));

        var submissionId = result.Data.Results.Single().SubmissionId;
        Assert.NotNull(submissionId);

        var storedFiles = await db.ExamSubmissionFiles
            .Where(x => x.ExamSubmissionId == submissionId)
            .OrderBy(x => x.QuestionLabel)
            .ToListAsync();

        Assert.Equal(2, storedFiles.Count);
        Assert.Equal($"uploads/{session.Code}/{topicB.Id:N}/HE186501_Alice/Q1/solution.zip", storedFiles[0].StorageRelativePath);
        Assert.Equal($"uploads/{session.Code}/{topicB.Id:N}/HE186501_Alice/Q2/solution.zip", storedFiles[1].StorageRelativePath);
    }

    [Fact]
    public async Task Handle_FailsEntryWhenTopicDoesNotBelongToSession()
    {
        await using var db = CreateDbContext(nameof(Handle_FailsEntryWhenTopicDoesNotBelongToSession));
        var session = CreateSession("SESSION-B");
        var topic = CreateTopic(session.Id, "Topic A", 1);

        db.ExamSessions.Add(session);
        db.ExamTopics.Add(topic);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out _);
        var command = new BatchSubmitZipsCommand(
            session.Id,
            new BatchSubmitZipsRequest
            {
                Entries =
                [
                    new StudentZipEntry
                    {
                        ExamTopicId = Guid.NewGuid(),
                        StudentCode = "HE186502",
                        StudentName = "Bob",
                        Q1Zip = CreateZipFormFile("q1.zip"),
                        Q2Zip = CreateZipFormFile("q2.zip")
                    }
                ]
            },
            BypassExamWindow: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var row = Assert.Single(result.Data!.Results);
        Assert.False(row.Success);
        Assert.Contains("ExamTopicId", row.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.ExamSubmissions.ToListAsync());
        Assert.Empty(await db.ExamSubmissionFiles.ToListAsync());
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        await using var db = CreateDbContext(nameof(Handle_ReturnsNotFound_WhenSessionDoesNotExist));
        var handler = CreateHandler(db, out _);
        
        var command = new BatchSubmitZipsCommand(Guid.NewGuid(), new BatchSubmitZipsRequest { Entries = [] }, false);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodeEnum.NotFound.ToString(), result.ErrorCode);
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenExamNotStartedAndBypassIsFalse()
    {
        await using var db = CreateDbContext(nameof(Handle_ReturnsError_WhenExamNotStartedAndBypassIsFalse));
        var session = CreateSession("SESSION-EARLY");
        session.StartsAtUtc = DateTime.UtcNow.AddHours(1);
        session.EndsAtUtc = DateTime.UtcNow.AddHours(2);
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out _);
        var command = new BatchSubmitZipsCommand(session.Id, new BatchSubmitZipsRequest { Entries = [] }, BypassExamWindow: false);
        
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodeEnum.BusinessRuleViolation.ToString(), result.ErrorCode);
        Assert.Contains("chưa bắt đầu", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenExamEndedAndBypassIsFalse()
    {
        await using var db = CreateDbContext(nameof(Handle_ReturnsError_WhenExamEndedAndBypassIsFalse));
        var session = CreateSession("SESSION-LATE");
        session.StartsAtUtc = DateTime.UtcNow.AddHours(-2);
        session.EndsAtUtc = DateTime.UtcNow.AddHours(-1);
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out _);
        var command = new BatchSubmitZipsCommand(session.Id, new BatchSubmitZipsRequest { Entries = [] }, BypassExamWindow: false);
        
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodeEnum.BusinessRuleViolation.ToString(), result.ErrorCode);
        Assert.Contains("đã kết thúc", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenSessionHasNoTopics()
    {
        await using var db = CreateDbContext(nameof(Handle_ReturnsError_WhenSessionHasNoTopics));
        var session = CreateSession("SESSION-NOTOPICS");
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out _);
        var command = new BatchSubmitZipsCommand(session.Id, new BatchSubmitZipsRequest { Entries = [] }, BypassExamWindow: true);
        
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodeEnum.BusinessRuleViolation.ToString(), result.ErrorCode);
        Assert.Contains("chưa có topic", result.Message);
    }

    [Fact]
    public async Task Handle_FailsEntry_WhenStudentAlreadySubmitted()
    {
        await using var db = CreateDbContext(nameof(Handle_FailsEntry_WhenStudentAlreadySubmitted));
        var session = CreateSession("SESSION-DUP");
        var topic = CreateTopic(session.Id, "Topic A", 1);
        
        db.ExamSessions.Add(session);
        db.ExamTopics.Add(topic);
        
        var existingSubmission = new ExamSubmission 
        { 
            Id = Guid.NewGuid(), 
            ExamSessionId = session.Id, 
            StudentCode = "HE111111", 
            StudentName = "DupStudent"
        };
        existingSubmission.InitializeEntity();
        db.ExamSubmissions.Add(existingSubmission);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, out _);
        var command = new BatchSubmitZipsCommand(
            session.Id,
            new BatchSubmitZipsRequest
            {
                Entries =
                [
                    new StudentZipEntry
                    {
                        ExamTopicId = topic.Id,
                        StudentCode = "HE111111",
                        StudentName = "DupStudent",
                        Q1Zip = CreateZipFormFile("q1.zip"),
                        Q2Zip = CreateZipFormFile("q2.zip")
                    }
                ]
            },
            BypassExamWindow: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Data!.Results);
        Assert.False(row.Success);
        Assert.Contains("đã nộp bài", row.Error, StringComparison.OrdinalIgnoreCase);
        // Ensure no extra submission was added
        Assert.Equal(1, await db.ExamSubmissions.CountAsync());
    }

    private static BatchSubmitZipsCommandHandler CreateHandler(
        PRN232_G9_AutoGradingToolDbContext db,
        out RecordingFileService fileService)
    {
        fileService = new RecordingFileService();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<SubmissionMappingProfile>()).CreateMapper();
        var uow = new UnitOfWork(db);
        return new BatchSubmitZipsCommandHandler(
            uow,
            new RecordingFileServiceFactory(fileService),
            mapper,
            NullLogger<BatchSubmitZipsCommandHandler>.Instance);
    }

    private static PRN232_G9_AutoGradingToolDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<PRN232_G9_AutoGradingToolDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new PRN232_G9_AutoGradingToolDbContext(options);
    }

    private static ExamSession CreateSession(string code)
    {
        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            Code = code,
            Title = "Demo Session",
            StartsAtUtc = DateTime.UtcNow.AddHours(-1),
            EndsAtUtc = DateTime.UtcNow.AddHours(1),
            ExamDurationMinutes = 90,
            Status = EntityStatusEnum.Active
        };
        session.InitializeEntity();
        return session;
    }

    private static ExamTopic CreateTopic(Guid sessionId, string title, int sortOrder)
    {
        var topic = new ExamTopic
        {
            Id = Guid.NewGuid(),
            ExamSessionId = sessionId,
            Title = title,
            SortOrder = sortOrder,
            Status = EntityStatusEnum.Active
        };
        topic.InitializeEntity();
        return topic;
    }

    private static IFormFile CreateZipFormFile(string fileName)
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip"
        };
    }

    private sealed class RecordingFileServiceFactory(RecordingFileService fileService) : IFileServiceFactory
    {
        public IFileService CreateFileService() => fileService;
    }

    private sealed class RecordingFileService : IFileService
    {
        public List<(string FileName, string SubDirectory)> Uploads { get; } = [];

        public Task<List<string?>> UploadFilesBulkAsync(List<IFormFile> files, List<string> fileNames, string subDirectory = "", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<bool>> DeleteFilesBulkAsync(List<string> filePaths, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> UploadFileAsync(IFormFile file, string fileName, string subDirectory = "", CancellationToken cancellationToken = default)
        {
            Uploads.Add((fileName, subDirectory));
            return Task.FromResult($"uploads/{subDirectory}/{fileName}");
        }

        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string subDirectory = "", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public string GetFileUrl(string filePath) => filePath;

        public Task<(byte[] FileContent, string ContentType)> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
