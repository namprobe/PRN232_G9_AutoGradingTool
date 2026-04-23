using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

internal static class TopicAwareSubmissionResolver
{
    public static TopicAwareSubmissionResolution Resolve(
        ExamSession session,
        IEnumerable<ExamSubmissionFile> submissionFiles)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submissionFiles);

        var files = submissionFiles.ToList();
        if (files.Count == 0)
            throw new InvalidOperationException("Submission does not contain any submission files.");

        var parsedFiles = files
            .Select(file =>
            {
                var pathInfo = SubmissionFilePathParser.Parse(file.StorageRelativePath, session.Code);

                if (!pathInfo.QuestionLabelFromPath.Equals(file.QuestionLabel, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Submission file '{file.StorageRelativePath}' points to '{pathInfo.QuestionLabelFromPath}' but record label is '{file.QuestionLabel}'.");
                }

                return (File: file, PathInfo: pathInfo);
            })
            .ToList();

        var topicIds = parsedFiles
            .Select(x => x.PathInfo.ExamTopicId)
            .Distinct()
            .ToList();

        if (topicIds.Count != 1)
            throw new InvalidOperationException("Submission files must belong to exactly one exam topic.");

        var studentFolders = parsedFiles
            .Select(x => x.PathInfo.StudentFolder)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (studentFolders.Count != 1)
            throw new InvalidOperationException("Submission files must belong to exactly one student folder.");

        var topic = session.Topics.FirstOrDefault(t => t.Id == topicIds[0])
            ?? throw new InvalidOperationException(
                $"Exam topic '{topicIds[0]}' from submission path does not exist in session '{session.Code}'.");

        var duplicateLabels = parsedFiles
            .GroupBy(x => x.File.QuestionLabel, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLabels.Count > 0)
            throw new InvalidOperationException(
                $"Submission contains duplicate question labels: {string.Join(", ", duplicateLabels)}.");

        var resolvedFiles = parsedFiles
            .Select(x =>
            {
                var question = topic.Questions.FirstOrDefault(q =>
                    q.Label.Equals(x.File.QuestionLabel, StringComparison.OrdinalIgnoreCase));

                if (question is null)
                {
                    throw new InvalidOperationException(
                        $"Question label '{x.File.QuestionLabel}' does not exist in topic '{topic.Id}'.");
                }

                return new TopicAwareSubmissionFileResolution(x.File, x.PathInfo, question);
            })
            .OrderBy(x => x.Question.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TopicAwareSubmissionResolution(topic, studentFolders[0], resolvedFiles);
    }
}

internal sealed record TopicAwareSubmissionResolution(
    ExamTopic Topic,
    string StudentFolder,
    IReadOnlyList<TopicAwareSubmissionFileResolution> Files);

internal sealed record TopicAwareSubmissionFileResolution(
    ExamSubmissionFile SubmissionFile,
    SubmissionFilePathInfo PathInfo,
    ExamQuestion Question);
