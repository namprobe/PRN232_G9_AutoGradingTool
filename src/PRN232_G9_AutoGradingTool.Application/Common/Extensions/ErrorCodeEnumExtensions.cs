using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Application.Common.Extensions;

/// <summary>
/// Extension methods for ErrorCodeEnum
/// </summary>
public static class ErrorCodeEnumExtensions
{
    /// <summary>
    /// Maps ErrorCodeEnum to HTTP status code
    /// </summary>
    public static int ToHttpStatusCode(this ErrorCodeEnum ErrorCodeEnum)
    {
        return ErrorCodeEnum switch
        {
            ErrorCodeEnum.Success => StatusCodes.Status200OK,
            
            // 400 Bad Request
            ErrorCodeEnum.ValidationFailed => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.InvalidInput => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.DuplicateEntry => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.InvalidOperation => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.InvalidFileType => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.FileSizeTooLarge => StatusCodes.Status400BadRequest,
            
            // 429 Too Many Requests
            ErrorCodeEnum.TooManyRequests => StatusCodes.Status429TooManyRequests,
            
            // 401 Unauthorized
            ErrorCodeEnum.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodeEnum.InvalidCredentials => StatusCodes.Status401Unauthorized,
            ErrorCodeEnum.TokenExpired => StatusCodes.Status401Unauthorized,
            ErrorCodeEnum.InvalidToken => StatusCodes.Status401Unauthorized,
            
            // 403 Forbidden
            ErrorCodeEnum.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodeEnum.InsufficientPermissions => StatusCodes.Status403Forbidden,
            
            // 404 Not Found
            ErrorCodeEnum.NotFound => StatusCodes.Status404NotFound,
        
            
            // 422 Unprocessable Entity
            ErrorCodeEnum.BusinessRuleViolation => StatusCodes.Status422UnprocessableEntity,
            ErrorCodeEnum.ResourceConflict => StatusCodes.Status422UnprocessableEntity,
            
            // 500 Internal Server Error
            ErrorCodeEnum.InternalError => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.DatabaseError => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.ExternalServiceError => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.FileUploadFailed => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.StorageError => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.FeatureDisabled => StatusCodes.Status503ServiceUnavailable,
            ErrorCodeEnum.InvalidResponse => StatusCodes.Status502BadGateway,
            
            // Email Errors
            ErrorCodeEnum.EmailSendFailed => StatusCodes.Status500InternalServerError,
            ErrorCodeEnum.EmailNotConfirmed => StatusCodes.Status403Forbidden,
            ErrorCodeEnum.EmailAlreadyConfirmed => StatusCodes.Status400BadRequest,
            ErrorCodeEnum.InvalidEmailToken => StatusCodes.Status400BadRequest,
            
            _ => StatusCodes.Status500InternalServerError
        };
    }
    
    /// <summary>
    /// Gets localized message for the error code using localization service
    /// </summary>
    /// <param name="errorCode">Error code to localize</param>
    /// <param name="localizationService">Localization service</param>
    /// <returns>Localized error message</returns>
    public static string GetLocalizedMessage(this ErrorCodeEnum errorCode, ILocalizationService localizationService)
    {
        return localizationService.GetErrorMessage(errorCode);
    }
    
    /// <summary>
    /// Gets localized message with format arguments for the error code
    /// </summary>
    /// <param name="errorCode">Error code to localize</param>
    /// <param name="localizationService">Localization service</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted error message</returns>
    public static string GetLocalizedMessage(this ErrorCodeEnum errorCode, ILocalizationService localizationService, params object[] args)
    {
        return localizationService.GetErrorMessage(errorCode, args);
    }
}