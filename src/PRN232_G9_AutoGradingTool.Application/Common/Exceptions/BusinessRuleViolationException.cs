using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

/// <summary>
/// Thrown when a business rule is violated.
/// Middleware maps this to 422 UnprocessableEntity via ErrorCodeEnum.BusinessRuleViolation.
/// </summary>
public class BusinessRuleViolationException : Exception
{
    public ErrorCodeEnum ErrorCode { get; }
    public List<string>? Details { get; }

    public BusinessRuleViolationException(ErrorCodeEnum errorCode = ErrorCodeEnum.BusinessRuleViolation, List<string>? details = null)
        : base(string.Empty)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public BusinessRuleViolationException(string message, ErrorCodeEnum errorCode = ErrorCodeEnum.BusinessRuleViolation, List<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}
