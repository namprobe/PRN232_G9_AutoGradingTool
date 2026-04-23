namespace PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

internal static class SubmissionFilePathParser
{
    public static SubmissionFilePathInfo Parse(string storageRelativePath, string expectedSessionCode)
    {
        if (string.IsNullOrWhiteSpace(storageRelativePath))
            throw new InvalidOperationException("StorageRelativePath is required.");

        if (string.IsNullOrWhiteSpace(expectedSessionCode))
            throw new InvalidOperationException("Expected session code is required.");

        var segments = storageRelativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sessionIndex = Array.FindIndex(
            segments,
            segment => segment.Equals(expectedSessionCode, StringComparison.OrdinalIgnoreCase));

        if (sessionIndex < 0)
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' does not contain exam session code '{expectedSessionCode}'.");

        if (segments.Length - sessionIndex != 5)
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' must follow '{expectedSessionCode}/{{topicId}}/{{studentFolder}}/{{questionLabel}}/solution.zip'.");

        if (!Guid.TryParse(segments[sessionIndex + 1], out var topicId))
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' contains invalid topicId '{segments[sessionIndex + 1]}'.");

        var studentFolder = segments[sessionIndex + 2];
        if (string.IsNullOrWhiteSpace(studentFolder))
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' does not contain a valid student folder.");

        var questionLabel = segments[sessionIndex + 3];
        if (string.IsNullOrWhiteSpace(questionLabel) ||
            !questionLabel.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' contains invalid question folder '{questionLabel}'.");
        }

        var fileName = segments[sessionIndex + 4];
        if (!fileName.Equals("solution.zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Storage path '{storageRelativePath}' must end with 'solution.zip'.");

        return new SubmissionFilePathInfo(
            segments[sessionIndex],
            topicId,
            studentFolder,
            questionLabel);
    }
}

internal sealed record SubmissionFilePathInfo(
    string SessionCode,
    Guid ExamTopicId,
    string StudentFolder,
    string QuestionLabelFromPath);
