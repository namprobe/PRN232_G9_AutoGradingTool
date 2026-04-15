using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Extensions;

/// <summary>
/// Extension methods for Result classes
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess) return StatusCodes.Status200OK;
        
        if (string.IsNullOrEmpty(result.ErrorCode) || !Enum.TryParse<ErrorCodeEnum>(result.ErrorCode, out var errorCode))
            return StatusCodes.Status500InternalServerError;
            
        return errorCode.ToHttpStatusCode();
    }
    
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode(this Result result)
    {
        if (result.IsSuccess) return StatusCodes.Status200OK;
        
        if (string.IsNullOrEmpty(result.ErrorCode) || !Enum.TryParse<ErrorCodeEnum>(result.ErrorCode, out var errorCode))
            return StatusCodes.Status500InternalServerError;
            
        return errorCode.ToHttpStatusCode();
    }
} 