namespace PRN232_G9_AutoGradingTool.Domain.Enums;

/// <summary>Loại testcase máy — hiện chỉ dùng Stub; mở rộng Http/Newman sau.</summary>
public enum GradingTestDefinitionKind
{
    Stub = 0,
    HttpRequest = 1,
    NewmanCollection = 2
}
