using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when user doesn't have sufficient permissions
/// </summary>
public class InsufficientPermissionsException : Exception
{
    public ErrorCodeEnum ErrorCode { get; }
    public List<string>? Details { get; }
    
    /// <summary>
    /// Create a new insufficient permissions exception with error code
    /// Message will be localized by GlobalExceptionHandlingMiddleware
    /// </summary>
    public InsufficientPermissionsException(ErrorCodeEnum errorCode = ErrorCodeEnum.InsufficientPermissions, List<string>? details = null)
        : base(string.Empty)
    {
        ErrorCode = errorCode;
        Details = details;
    }
    
    /// <summary>
    /// Create a new insufficient permissions exception with custom message
    /// </summary>
    public InsufficientPermissionsException(string message, ErrorCodeEnum errorCode = ErrorCodeEnum.InsufficientPermissions, List<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}
