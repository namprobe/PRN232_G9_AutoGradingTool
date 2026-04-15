namespace PRN232_G9_AutoGradingTool.Application.Common.Attributes;

/// <summary>
/// Attribute to enforce authorization on a CQRS request
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Comma-separated list of roles
    /// </summary>
    public string Roles { get; set; } = string.Empty;
} 