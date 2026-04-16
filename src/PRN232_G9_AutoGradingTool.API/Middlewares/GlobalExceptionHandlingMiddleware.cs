using System.Net;
using System.Text.Json;
using System.Data.Common;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Exceptions;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.API.Middlewares;

/// <summary>
/// Middleware for handling exceptions globally with localization support
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IStringLocalizer _localizer;
    
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IStringLocalizerFactory localizerFactory
    )
    {
        _next = next;
        _logger = logger;
        var errorMessagesType = typeof(Application.Resources.ErrorMessages);
        _localizer = localizerFactory.Create(errorMessagesType);
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ShouldLogDetailedError(ex))
            {
                 _logger.LogError(ex, 
                    "Unhandled exception. Path: {Path}, Method: {Method}, User: {User}", 
                context.Request.Path, 
                context.Request.Method,
                    context.User?.Identity?.Name ?? "Anonymous");
            }
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, errors) = ClassifyException(exception);
        
        context.Response.StatusCode = (int)statusCode;
        var result = Result.Failure(message, errorCode, errors);
        context.Response.ContentType = "application/json";
        
        return context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
    
    private (HttpStatusCode statusCode, ErrorCodeEnum errorCode, string message, List<string>? errors) ClassifyException(Exception exception)
    {
        // Validation exceptions
        if (exception is FluentValidation.ValidationException fluentEx)
            return (HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationFailed, 
                GetLocalizedErrorMessage(ErrorCodeEnum.ValidationFailed),
                fluentEx.Errors.Select(e => e.ErrorMessage).ToList());
            
        if (exception is ValidationException validationEx)
            return (HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationFailed, 
                GetLocalizedErrorMessage(ErrorCodeEnum.ValidationFailed),
                validationEx.Errors.SelectMany(e => e.Value).ToList());
        
        // Custom application exceptions (with ErrorCodeEnum)
        if (exception is InvalidTokenException invalidTokenEx)
            return HandleCustomException(invalidTokenEx, HttpStatusCode.Unauthorized);
        
        if (exception is ForbiddenAccessException forbiddenEx)
            return HandleCustomException(forbiddenEx, HttpStatusCode.Forbidden);
        
        if (exception is InsufficientPermissionsException insufficientEx)
            return (HttpStatusCode.Forbidden, insufficientEx.ErrorCode,
                GetMessageOrDefault(insufficientEx.Message, insufficientEx.ErrorCode),
                insufficientEx.Details);
        
        if (exception is BusinessRuleViolationException brEx)
            return (HttpStatusCode.UnprocessableEntity, brEx.ErrorCode,
                GetMessageOrDefault(brEx.Message, brEx.ErrorCode),
                brEx.Details);
        
        if (exception is NotFoundException notFoundEx)
            return (HttpStatusCode.NotFound, notFoundEx.ErrorCode,
                GetMessageOrDefault(notFoundEx.Message, notFoundEx.ErrorCode), null);
        
        // System-level exceptions
        if (exception is UnauthorizedAccessException unauthorizedAccessEx)
            return HandleUnauthorizedAccessException(unauthorizedAccessEx);
        
        if (exception is KeyNotFoundException keyNotFoundEx)
            return (HttpStatusCode.NotFound, ErrorCodeEnum.NotFound,
                GetMessageOrDefault(keyNotFoundEx.Message, ErrorCodeEnum.NotFound), null);
        
        if (exception is ArgumentException)
            return (HttpStatusCode.BadRequest, ErrorCodeEnum.InvalidInput,
                GetMessageOrDefault(exception.Message, ErrorCodeEnum.InvalidInput), null);
        
        if (exception is InvalidOperationException invalidEx)
            return HandleInvalidOperationException(invalidEx);
        
        // Database exceptions
        if (exception is NpgsqlException || exception is DbUpdateException)
            return HandleDatabaseException(exception);
        
        // File system exceptions
        if (exception is FileNotFoundException)
            return (HttpStatusCode.NotFound, ErrorCodeEnum.FileNotFound,
                GetLocalizedExceptionMessage("Exception_FileNotFound"), null);
        
        if (exception is DirectoryNotFoundException)
            return (HttpStatusCode.NotFound, ErrorCodeEnum.FileNotFound,
                GetLocalizedExceptionMessage("Exception_DirectoryNotFound"), null);
        
        // External service exceptions
        if (exception is TimeoutException)
            return (HttpStatusCode.RequestTimeout, ErrorCodeEnum.ExternalServiceError,
                GetLocalizedExceptionMessage("Exception_Timeout"), null);
        
        if (exception is HttpRequestException)
            return (HttpStatusCode.BadGateway, ErrorCodeEnum.ExternalServiceError,
                GetLocalizedExceptionMessage("Exception_ExternalServiceUnavailable"), null);
        
        // Default: Internal server error
        return (HttpStatusCode.InternalServerError, ErrorCodeEnum.InternalError,
            GetLocalizedErrorMessage(ErrorCodeEnum.InternalError), null);
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandleCustomException(Exception ex, HttpStatusCode statusCode)
    {
        var errorCode = ex switch
        {
            InvalidTokenException i => i.ErrorCode,
            ForbiddenAccessException f => f.ErrorCode,
            _ => ErrorCodeEnum.InternalError
        };
        
        return (statusCode, errorCode, GetMessageOrDefault(ex.Message, errorCode), null);
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandleUnauthorizedAccessException(UnauthorizedAccessException ex)
    {
        if (IsFileAccess(ex))
        {
            return (HttpStatusCode.Forbidden, ErrorCodeEnum.StorageError,
                GetMessageOrDefault(ex.Message, "Exception_FileAccessDenied"), null);
        }
        
        // Auth case: luôn sử dụng message từ resource theo ErrorCode_Unauthorized,
        // không dùng ex.Message (mặc định tiếng Anh: "Attempted to perform an unauthorized operation.")
        return (HttpStatusCode.Unauthorized, ErrorCodeEnum.Unauthorized,
            GetLocalizedErrorMessage(ErrorCodeEnum.Unauthorized), null);
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandleInvalidOperationException(InvalidOperationException ex)
    {
        if (HasDatabaseInnerException(ex))
        {
            return (HttpStatusCode.InternalServerError, ErrorCodeEnum.DatabaseError,
                GetLocalizedErrorMessage(ErrorCodeEnum.DatabaseError), null);
        }
        
        return (HttpStatusCode.BadRequest, ErrorCodeEnum.InvalidOperation,
            GetLocalizedExceptionMessage("Exception_InvalidOperation"), null);
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandleDatabaseException(Exception exception)
        {
        if (exception is NpgsqlException pgEx && !string.IsNullOrEmpty(pgEx.SqlState))
            {
            return pgEx.SqlState switch
            {
                var state when state.StartsWith("08") => // Connection errors
                    (HttpStatusCode.ServiceUnavailable, ErrorCodeEnum.DatabaseError,
                     GetLocalizedExceptionMessage("Exception_DatabaseUnavailable"), null),
                
                var state when state.StartsWith("28") => // Authentication failures
                    (HttpStatusCode.ServiceUnavailable, ErrorCodeEnum.DatabaseError,
                     GetLocalizedExceptionMessage("Exception_DatabaseAuthFailed"), null),
                
                "23505" => // Unique violation
                    (HttpStatusCode.Conflict, ErrorCodeEnum.DuplicateEntry,
                     GetLocalizedErrorMessage(ErrorCodeEnum.DuplicateEntry), null),
                
                "23503" => // Foreign key violation
                    (HttpStatusCode.Conflict, ErrorCodeEnum.ResourceConflict,
                     GetLocalizedExceptionMessage("Exception_DataConstraintViolation"), null),
                
                "23502" => // Not null violation
                    (HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationFailed,
                     GetLocalizedExceptionMessage("Exception_DataConstraintViolation"), null),
                
                var state when state.StartsWith("23") => // Other integrity violations
                    (HttpStatusCode.Conflict, ErrorCodeEnum.ResourceConflict,
                     GetLocalizedExceptionMessage("Exception_DataConstraintViolation"), null),
                
                "57014" => // Query timeout
                    (HttpStatusCode.RequestTimeout, ErrorCodeEnum.DatabaseError,
                     GetLocalizedExceptionMessage("Exception_DatabaseTimeout"), null),
                
                var state when state.StartsWith("53") => // Insufficient resources
                    (HttpStatusCode.ServiceUnavailable, ErrorCodeEnum.DatabaseError,
                     GetLocalizedExceptionMessage("Exception_DatabaseUnavailable"), null),
                
                var state when state.StartsWith("40") => // Transaction rollback
                    (HttpStatusCode.Conflict, ErrorCodeEnum.ResourceConflict,
                     GetLocalizedExceptionMessage("Exception_DataUpdateConflict"), null),
                
                _ => (HttpStatusCode.InternalServerError, ErrorCodeEnum.DatabaseError,
                      GetLocalizedErrorMessage(ErrorCodeEnum.DatabaseError), null)
            };
        }
        
        if (exception is DbUpdateException)
        {
            return (HttpStatusCode.Conflict, ErrorCodeEnum.ResourceConflict,
                GetLocalizedExceptionMessage("Exception_DataUpdateConflict"), null);
        }
        
        return (HttpStatusCode.InternalServerError, ErrorCodeEnum.DatabaseError,
            GetLocalizedErrorMessage(ErrorCodeEnum.DatabaseError), null);
    }
    
    private bool HasDatabaseInnerException(Exception exception)
    {
        if (exception.InnerException == null) return false;
        
        var innerEx = exception.InnerException;
        if (innerEx is NpgsqlException || innerEx is DbUpdateException || innerEx is DbException)
            return true;
        
        if (innerEx.InnerException != null)
            return HasDatabaseInnerException(innerEx);
        
        // Fallback: Check message for EF Core transient failure patterns
        var message = exception.Message.ToLowerInvariant();
        return (message.Contains("transient failure") || message.Contains("likely due to a transient failure")) &&
               (message.Contains("database") || message.Contains("connection") || 
                message.Contains("entity framework") || message.Contains("dbcontext"));
    }
    
    private bool IsFileAccess(Exception exception)
    {
        var message = exception.Message.ToLowerInvariant();
        return message.Contains("file") || message.Contains("directory") || message.Contains("path");
    }
    
    private bool ShouldLogDetailedError(Exception exception)
    {
        // Don't log user-related errors
        if (exception is ValidationException or FluentValidation.ValidationException or 
            ArgumentException or UnauthorizedAccessException or ForbiddenAccessException or 
            KeyNotFoundException or InvalidTokenException)
            return false;
        
        // InvalidOperationException: only log if database-related
        if (exception is InvalidOperationException invalidEx)
            return HasDatabaseInnerException(invalidEx);
        
        return true; // Log all other exceptions
    }
    
    private string GetMessageOrDefault(string? message, ErrorCodeEnum errorCode)
    {
        return string.IsNullOrEmpty(message) 
            ? GetLocalizedErrorMessage(errorCode) 
            : message;
    }
    
    private string GetMessageOrDefault(string? message, string exceptionKey)
    {
        return string.IsNullOrEmpty(message) 
            ? GetLocalizedExceptionMessage(exceptionKey) 
            : message;
    }
    
    private string GetLocalizedErrorMessage(ErrorCodeEnum errorCode)
    {
        var key = errorCode.ToErrorCodeKey();
        var localizedString = _localizer[key];
        
        if (localizedString.ResourceNotFound)
        {
            _logger.LogWarning("Missing localization for error code: {ErrorCode}", errorCode);
            return errorCode.ToString();
        }
        
        return localizedString.Value;
    }
    
    private string GetLocalizedExceptionMessage(string exceptionKey)
    {
        var localizedString = _localizer[exceptionKey];
        
        if (localizedString.ResourceNotFound)
        {
            _logger.LogWarning("Missing localization for exception key: {ExceptionKey}", exceptionKey);
            return exceptionKey;
        }
        
        return localizedString.Value;
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
} 
