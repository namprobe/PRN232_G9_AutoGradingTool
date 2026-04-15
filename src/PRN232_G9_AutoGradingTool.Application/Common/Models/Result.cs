using System.Text.Json.Serialization;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Application.Common.Models;

public class Result<T>
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; } = null;

    public static Result<T> Success(T data, string message = "Success")
    {
        return new Result<T> { IsSuccess = true, Message = message, Data = data };
    }

    public static Result<T> Failure(string message, ErrorCodeEnum errorCode, List<string>? errors = null)
    {
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }

    public static Result<T> Failure(string message, ErrorCodeEnum errorCode)
    {
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString() };
    }
    
    /// <summary>
    /// Creates a failure result with localized message
    /// </summary>
    public static Result<T> Failure(ErrorCodeEnum errorCode, ILocalizationService localizationService, List<string>? errors = null)
    {
        var message = localizationService.GetErrorMessage(errorCode);
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }
    
    /// <summary>
    /// Creates a failure result with localized message and format arguments
    /// </summary>
    public static Result<T> FailureWithArgs(ErrorCodeEnum errorCode, ILocalizationService localizationService, object[] messageArgs, List<string>? errors = null)
    {
        var message = localizationService.GetErrorMessage(errorCode, messageArgs);
        return new Result<T> { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }
    
    /// <summary>
    /// Creates a success result with localized message
    /// </summary>
    public static Result<T> Success(T data, ILocalizationService localizationService, string messageKey = "Success_Default")
    {
        var message = localizationService.GetSuccessMessage(messageKey);
        return new Result<T> { IsSuccess = true, Message = message, Data = data };
    }
}

public class Result
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; set; } = null;

    public static Result Success(string message = "Success")
    {
        return new Result { IsSuccess = true, Message = message };
    }

    public static Result Failure(string message, ErrorCodeEnum errorCode, List<string>? errors = null)
    {
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }

    public static Result Failure(string message, ErrorCodeEnum errorCode)
    {
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString()};
    }
    
    /// <summary>
    /// Creates a failure result with localized message
    /// </summary>
    public static Result Failure(ErrorCodeEnum errorCode, ILocalizationService localizationService, List<string>? errors = null)
    {
        var message = localizationService.GetErrorMessage(errorCode);
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }
    
    /// <summary>
    /// Creates a failure result with localized message and format arguments
    /// </summary>
    public static Result FailureWithArgs(ErrorCodeEnum errorCode, ILocalizationService localizationService, object[] messageArgs, List<string>? errors = null)
    {
        var message = localizationService.GetErrorMessage(errorCode, messageArgs);
        return new Result { IsSuccess = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };
    }
    
    /// <summary>
    /// Creates a success result with localized message
    /// </summary>
    public static Result Success(ILocalizationService localizationService, string messageKey = "Success_Default")
    {
        var message = localizationService.GetSuccessMessage(messageKey);
        return new Result { IsSuccess = true, Message = message };
    }
}