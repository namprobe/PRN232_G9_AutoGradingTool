namespace PRN232_G9_AutoGradingTool.API.Configurations;

/// <summary>
/// Một endpoint = một nhóm Swagger (sắp xếp theo tiền tố số trong tên tag).
/// </summary>
public static class SwaggerTagResolver
{
    public const string Auth = "01 — CMS · Đăng nhập & phiên làm việc";
    public const string Semester = "02 — CMS · Học kỳ";
    public const string ExamSession = "03 — CMS · Ca thi (phiên)";
    public const string ExamClass = "04 — CMS · Lớp học (SE…, sĩ số)";
    public const string SessionClassBatch = "05 — CMS · Lớp trong ca & chấm batch";
    public const string Topic = "06a — CMS · Chủ đề (topic)";
    public const string Question = "06b — CMS · Câu hỏi";
    public const string TestCase = "06c — CMS · Testcase (rubric)";
    public const string GradingPack = "07 — CMS · Grading pack & assets";
    public const string Submissions = "08 — CMS · Bài nộp & chấm lại";
    public const string StudentSubmit = "09 — Student · Nộp bài (ZIP)";
    public const string Other = "99 — API khác";

    /// <param name="relativePath">Giá trị <see cref="Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription.RelativePath"/> (vd: api/cms/grading/semesters).</param>
    public static string Resolve(string? relativePath)
    {
        var path = relativePath?.ToLowerInvariant() ?? "";

        if (path.StartsWith("api/cms/auth"))
            return Auth;

        if (path.StartsWith("api/student/grading"))
            return StudentSubmit;

        if (!path.StartsWith("api/cms/grading/"))
            return Other;

        // Thứ tự: cụ thể trước, tổng quát sau
        if (path.Contains("exam-session-classes") || path.Contains("/session-classes"))
            return SessionClassBatch;

        if (path.Contains("/exam-classes"))
            return ExamClass;

        if (path.Contains("grading-packs") || path.Contains("pack-assets"))
            return GradingPack;

        if (path.Contains("exam-test-cases"))
            return TestCase;

        if (path.Contains("exam-questions"))
            return Question;

        if (path.Contains("exam-topics") || path.Contains("/topics"))
            return Topic;

        if (path.Contains("submissions"))
            return Submissions;

        if (path.Contains("semesters") && !path.Contains("exam-classes"))
            return Semester;

        if (path.Contains("exam-sessions"))
            return ExamSession;

        return Other;
    }
}
