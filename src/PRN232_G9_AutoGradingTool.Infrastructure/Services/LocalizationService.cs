using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

/// <summary>
/// Service for localizing messages with three-level fallback mechanism
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer _errorLocalizer;
    private readonly IStringLocalizer _successLocalizer;
    private readonly IStringLocalizer _validationLocalizer;
    private readonly IStringLocalizer _commonLocalizer;
    private readonly ILogger<LocalizationService> _logger;

    public LocalizationService(
        IStringLocalizerFactory localizerFactory,
        ILogger<LocalizationService> logger)
    {
        _logger = logger;
        
        // Create localizers for each resource file
        var errorMessagesType = typeof(Application.Resources.ErrorMessages);
        var successMessagesType = typeof(Application.Resources.SuccessMessages);
        var validationMessagesType = typeof(Application.Resources.ValidationMessages);
        var commonMessagesType = typeof(Application.Resources.CommonMessages);
        
        _errorLocalizer = localizerFactory.Create(errorMessagesType);
        _successLocalizer = localizerFactory.Create(successMessagesType);
        _validationLocalizer = localizerFactory.Create(validationMessagesType);
        _commonLocalizer = localizerFactory.Create(commonMessagesType);
    }

    /// <inheritdoc/>
    public string GetErrorMessage(ErrorCodeEnum errorCode)
    {
        var key = errorCode.ToErrorCodeKey(); // Uses extension method for consistency
        return GetLocalizedString(_errorLocalizer, key, errorCode.ToString());
    }

    /// <inheritdoc/>
    public string GetErrorMessage(ErrorCodeEnum errorCode, params object[] args)
    {
        var key = errorCode.ToErrorCodeKey(); // Uses extension method for consistency
        var message = GetLocalizedString(_errorLocalizer, key, errorCode.ToString());
        
        try
        {
            return string.Format(message, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized message for key: {Key}", key);
            return message;
        }
    }

    /// <inheritdoc/>
    public string GetExceptionMessage(string exceptionKey)
    {
        return GetLocalizedString(_errorLocalizer, exceptionKey, exceptionKey);
    }

    /// <inheritdoc/>
    public string GetExceptionMessage(string exceptionKey, params object[] args)
    {
        var message = GetLocalizedString(_errorLocalizer, exceptionKey, exceptionKey);
        
        try
        {
            return string.Format(message, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized exception message for key: {Key}", exceptionKey);
            return message;
        }
    }

    /// <inheritdoc/>
    public string GetSuccessMessage(string messageKey)
    {
        return GetLocalizedString(_successLocalizer, messageKey, messageKey);
    }

    /// <inheritdoc/>
    public string GetSuccessMessage(string messageKey, params object[] args)
    {
        var message = GetLocalizedString(_successLocalizer, messageKey, messageKey);
        
        try
        {
            return string.Format(message, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized success message for key: {Key}", messageKey);
            return message;
        }
    }

    /// <inheritdoc/>
    public string GetValidationMessage(string messageKey)
    {
        return GetLocalizedString(_validationLocalizer, messageKey, messageKey);
    }

    /// <inheritdoc/>
    public string GetValidationMessage(string messageKey, params object[] args)
    {
        var message = GetLocalizedString(_validationLocalizer, messageKey, messageKey);
        
        try
        {
            return string.Format(message, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized validation message for key: {Key}", messageKey);
            return message;
        }
    }

    /// <inheritdoc/>
    public string GetCommonMessage(string messageKey)
    {
        return GetLocalizedString(_commonLocalizer, messageKey, messageKey);
    }

    /// <inheritdoc/>
    public string GetCommonMessage(string messageKey, params object[] args)
    {
        var message = GetLocalizedString(_commonLocalizer, messageKey, messageKey);
        
        try
        {
            return string.Format(message, args);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format localized common message for key: {Key}", messageKey);
            return message;
        }
    }

    /// <summary>
    /// Gets localized string with three-level fallback mechanism
    /// Level 1: Requested culture (e.g., vi)
    /// Level 2: Default culture (en)
    /// Level 3: Fallback value
    /// </summary>
    private string GetLocalizedString(IStringLocalizer localizer, string key, string fallbackValue)
    {
        var localizedString = localizer[key];
        
        // Check if resource was found (ResourceNotFound = true means not found)
        if (localizedString.ResourceNotFound)
        {
            _logger.LogWarning(
                "Missing localization for key: {Key}, Culture: {Culture}. Using fallback: {Fallback}", 
                key, 
                CultureInfo.CurrentCulture.Name, 
                fallbackValue);
            
            // Level 3: Return fallback value
            return fallbackValue;
        }
        
        // Level 1 or 2: Return localized value
        return localizedString.Value;
    }
}
