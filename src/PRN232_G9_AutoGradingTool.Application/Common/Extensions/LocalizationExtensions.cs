using System.Text;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Extensions;

/// <summary>
/// Extension methods for localization key generation
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Converts ErrorCodeEnum to resource key format: ErrorCode_{ErrorCode}
    /// Example: ErrorCodeEnum.Unauthorized -> "ErrorCode_Unauthorized"
    /// </summary>
    public static string ToErrorCodeKey(this ErrorCodeEnum errorCode)
    {
        return $"ErrorCode_{errorCode}";
    }
    
    /// <summary>
    /// Converts ErrorCodeEnum to snake_case resource key format: ErrorCode_{error_code}
    /// Example: ErrorCodeEnum.InvalidCredentials -> "ErrorCode_InvalidCredentials"
    /// Note: Currently uses PascalCase as ErrorCodeEnum values are PascalCase
    /// </summary>
    public static string ToErrorCodeKeySnakeCase(this ErrorCodeEnum errorCode)
    {
        var snakeCase = ToSnakeCase(errorCode.ToString());
        return $"ErrorCode_{snakeCase}";
    }
    
    /// <summary>
    /// Converts exception type name to resource key format: Exception_{ExceptionName}
    /// Example: "InvalidTokenException" -> "Exception_InvalidToken"
    /// </summary>
    public static string ToExceptionKey(this Type exceptionType)
    {
        var exceptionName = exceptionType.Name;
        // Remove "Exception" suffix if present
        if (exceptionName.EndsWith("Exception"))
        {
            exceptionName = exceptionName.Substring(0, exceptionName.Length - "Exception".Length);
        }
        return $"Exception_{exceptionName}";
    }
    
    /// <summary>
    /// Converts exception type name to snake_case resource key format
    /// Example: "InvalidTokenException" -> "Exception_invalid_token"
    /// </summary>
    public static string ToExceptionKeySnakeCase(this Type exceptionType)
    {
        var exceptionName = exceptionType.Name;
        // Remove "Exception" suffix if present
        if (exceptionName.EndsWith("Exception"))
        {
            exceptionName = exceptionName.Substring(0, exceptionName.Length - "Exception".Length);
        }
        var snakeCase = ToSnakeCase(exceptionName);
        return $"Exception_{snakeCase}";
    }
    
    /// <summary>
    /// Converts PascalCase string to snake_case
    /// Example: "InvalidCredentials" -> "invalid_credentials"
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));
        
        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Converts PascalCase string to camelCase
    /// Example: "InvalidCredentials" -> "invalidCredentials"
    /// </summary>
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        if (input.Length == 1)
            return char.ToLowerInvariant(input[0]).ToString();
            
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}
