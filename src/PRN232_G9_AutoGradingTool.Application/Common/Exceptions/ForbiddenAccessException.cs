using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when access is forbidden
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ErrorCodeEnum ErrorCode { get; }
    
    /// <summary>
    /// Create a new forbidden access exception with error code
    /// Message will be localized by GlobalExceptionHandlingMiddleware
    /// </summary>
    public ForbiddenAccessException(ErrorCodeEnum errorCode = ErrorCodeEnum.Forbidden)
        : base(string.Empty)
    {
        ErrorCode = errorCode;
    }
    
    /// <summary>
    /// Create a new forbidden access exception with custom message
    /// </summary>
    public ForbiddenAccessException(string message, ErrorCodeEnum errorCode = ErrorCodeEnum.Forbidden)
        : base(message)
    {
        ErrorCode = errorCode;
    }
} 