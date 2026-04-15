using PRN232_G9_AutoGradingTool.Application.Common.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Service for localizing messages based on current culture
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets localized error message for the given error code
    /// </summary>
    /// <param name="errorCode">Error code to localize</param>
    /// <returns>Localized error message</returns>
    string GetErrorMessage(ErrorCodeEnum errorCode);
    
    /// <summary>
    /// Gets localized error message with format arguments
    /// </summary>
    /// <param name="errorCode">Error code to localize</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted error message</returns>
    string GetErrorMessage(ErrorCodeEnum errorCode, params object[] args);
    
    /// <summary>
    /// Gets localized exception message for a specific exception type
    /// </summary>
    /// <param name="exceptionKey">Exception key (e.g., "Exception_ArgumentException")</param>
    /// <returns>Localized exception message</returns>
    string GetExceptionMessage(string exceptionKey);
    
    /// <summary>
    /// Gets localized exception message with format arguments
    /// </summary>
    /// <param name="exceptionKey">Exception key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted exception message</returns>
    string GetExceptionMessage(string exceptionKey, params object[] args);
    
    /// <summary>
    /// Gets localized success message
    /// </summary>
    /// <param name="messageKey">Success message key (e.g., "Success_Created")</param>
    /// <returns>Localized success message</returns>
    string GetSuccessMessage(string messageKey);
    
    /// <summary>
    /// Gets localized success message with format arguments
    /// </summary>
    /// <param name="messageKey">Success message key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted success message</returns>
    string GetSuccessMessage(string messageKey, params object[] args);
    
    /// <summary>
    /// Gets localized validation message
    /// </summary>
    /// <param name="messageKey">Validation message key</param>
    /// <returns>Localized validation message</returns>
    string GetValidationMessage(string messageKey);
    
    /// <summary>
    /// Gets localized validation message with format arguments
    /// </summary>
    /// <param name="messageKey">Validation message key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted validation message</returns>
    string GetValidationMessage(string messageKey, params object[] args);
    
    /// <summary>
    /// Gets localized common message
    /// </summary>
    /// <param name="messageKey">Common message key</param>
    /// <returns>Localized common message</returns>
    string GetCommonMessage(string messageKey);
    
    /// <summary>
    /// Gets localized common message with format arguments
    /// </summary>
    /// <param name="messageKey">Common message key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted common message</returns>
    string GetCommonMessage(string messageKey, params object[] args);
}
