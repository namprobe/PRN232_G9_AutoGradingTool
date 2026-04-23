using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Jobs;
using Xunit;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Tests;

public class SubmissionPathResolutionTests
{
    [Fact]
    public void Parse_ExtractsTopicAndQuestionFromStoragePath()
    {
        var topicId = Guid.NewGuid();

        var parsed = SubmissionFilePathParser.Parse(
            $"uploads/SESSION-01/{topicId:N}/HE186501_Alice/Q2/solution.zip",
            "SESSION-01");

        Assert.Equal("SESSION-01", parsed.SessionCode);
        Assert.Equal(topicId, parsed.ExamTopicId);
        Assert.Equal("HE186501_Alice", parsed.StudentFolder);
        Assert.Equal("Q2", parsed.QuestionLabelFromPath);
    }

    [Fact]
    public void Parse_ThrowsWhenTopicIdSegmentIsInvalid()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SubmissionFilePathParser.Parse(
                "uploads/SESSION-02/not-a-guid/HE186501_Alice/Q1/solution.zip",
                "SESSION-02"));

        Assert.Contains("invalid topicId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenPathIsMissingRequiredSegments()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SubmissionFilePathParser.Parse(
                "uploads/SESSION-02/HE186501_Alice/Q1/solution.zip",
                "SESSION-02"));

        Assert.Contains("must follow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_UsesTopicFromPathAndMapsQuestionsInsideThatTopic()
    {
        var topicA = CreateTopic("Topic A", 1);
        var topicB = CreateTopic("Topic B", 2);
        topicA.Questions.Add(CreateQuestion(topicA.Id, "Q1"));
        topicA.Questions.Add(CreateQuestion(topicA.Id, "Q2"));
        topicB.Questions.Add(CreateQuestion(topicB.Id, "Q1"));

        var session = CreateSession("SESSION-03", topicA, topicB);
        var files = new[]
        {
            CreateSubmissionFile(topicA.Id, "SESSION-03", "HE186501_Alice", "Q1"),
            CreateSubmissionFile(topicA.Id, "SESSION-03", "HE186501_Alice", "Q2")
        };

        var resolution = TopicAwareSubmissionResolver.Resolve(session, files);

        Assert.Equal(topicA.Id, resolution.Topic.Id);
        Assert.Equal("HE186501_Alice", resolution.StudentFolder);
        Assert.Equal(2, resolution.Files.Count);
        Assert.All(resolution.Files, x => Assert.Equal(topicA.Id, x.Question.ExamTopicId));
        Assert.Equal(["Q1", "Q2"], resolution.Files.Select(x => x.Question.Label).ToArray());
    }

    [Fact]
    public void Resolve_ThrowsWhenSubmissionMixesTopics()
    {
        var topicA = CreateTopic("Topic A", 1);
        var topicB = CreateTopic("Topic B", 2);
        topicA.Questions.Add(CreateQuestion(topicA.Id, "Q1"));
        topicB.Questions.Add(CreateQuestion(topicB.Id, "Q2"));

        var session = CreateSession("SESSION-04", topicA, topicB);
        var files = new[]
        {
            CreateSubmissionFile(topicA.Id, "SESSION-04", "HE186501_Alice", "Q1"),
            CreateSubmissionFile(topicB.Id, "SESSION-04", "HE186501_Alice", "Q2")
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TopicAwareSubmissionResolver.Resolve(session, files));

        Assert.Contains("exactly one exam topic", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenQuestionLabelInPathDiffersFromRecord()
    {
        var topicA = CreateTopic("Topic A", 1);
        topicA.Questions.Add(CreateQuestion(topicA.Id, "Q1"));
        var session = CreateSession("SESSION-05", topicA);

        var file = new ExamSubmissionFile
        {
            ExamSubmissionId = Guid.NewGuid(),
            QuestionLabel = "Q2",
            StorageRelativePath = $"uploads/SESSION-05/{topicA.Id:N}/HE186501_Alice/Q1/solution.zip",
            OriginalFileName = "demo-q1.zip",
            Status = EntityStatusEnum.Active
        };
        file.InitializeEntity();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TopicAwareSubmissionResolver.Resolve(session, [file]));

        Assert.Contains("record label", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ThrowsWhenPathTopicDoesNotExistInSession()
    {
        var topicA = CreateTopic("Topic A", 1);
        topicA.Questions.Add(CreateQuestion(topicA.Id, "Q1"));
        var session = CreateSession("SESSION-06", topicA);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TopicAwareSubmissionResolver.Resolve(
                session,
                [
                    CreateSubmissionFile(Guid.NewGuid(), "SESSION-06", "HE186501_Alice", "Q1")
                ]));

        Assert.Contains("does not exist in session", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ExamSession CreateSession(string code, params ExamTopic[] topics)
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
            Status = EntityStatusEnum.Active,
            Topics = topics.ToList()
        };
        session.InitializeEntity();
        return session;
    }

    private static ExamTopic CreateTopic(string title, int sortOrder)
    {
        var topic = new ExamTopic
        {
            Id = Guid.NewGuid(),
            ExamSessionId = Guid.NewGuid(),
            Title = title,
            SortOrder = sortOrder,
            Status = EntityStatusEnum.Active
        };
        topic.InitializeEntity();
        return topic;
    }

    private static ExamQuestion CreateQuestion(Guid topicId, string label)
    {
        var question = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            ExamTopicId = topicId,
            Label = label,
            Title = label,
            MaxScore = 5,
            Status = EntityStatusEnum.Active
        };
        question.InitializeEntity();
        return question;
    }

    private static ExamSubmissionFile CreateSubmissionFile(Guid topicId, string sessionCode, string studentFolder, string label)
    {
        var file = new ExamSubmissionFile
        {
            Id = Guid.NewGuid(),
            ExamSubmissionId = Guid.NewGuid(),
            QuestionLabel = label,
            StorageRelativePath = $"uploads/{sessionCode}/{topicId:N}/{studentFolder}/{label}/solution.zip",
            OriginalFileName = $"{label}.zip",
            Status = EntityStatusEnum.Active
        };
        file.InitializeEntity();
        return file;
    }
}
