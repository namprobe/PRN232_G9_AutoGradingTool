using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when JWT token is invalid or expired
/// </summary>
public class InvalidTokenException : Exception
{
    public ErrorCodeEnum ErrorCode { get; }
    
    /// <summary>
    /// Create a new invalid token exception with error code
    /// Message will be localized by GlobalExceptionHandlingMiddleware
    /// </summary>
    public InvalidTokenException(ErrorCodeEnum errorCode = ErrorCodeEnum.InvalidToken)
        : base(string.Empty)
    {
        ErrorCode = errorCode;
    }
    
    /// <summary>
    /// Create a new invalid token exception with custom message
    /// </summary>
    public InvalidTokenException(string message, ErrorCodeEnum errorCode = ErrorCodeEnum.InvalidToken)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
