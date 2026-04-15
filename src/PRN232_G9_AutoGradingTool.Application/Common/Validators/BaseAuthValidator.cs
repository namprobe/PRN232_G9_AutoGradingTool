using FluentValidation;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Application.Common.Validators;

/// <summary>
/// Base validator class for authentication-related commands
/// Provides common validation logic and dependency injection
/// </summary>
/// <typeparam name="T">The command type to validate</typeparam>
public abstract class BaseAuthValidator<T> : AbstractValidator<T>
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly ILocalizationService LocalizationService;

    protected BaseAuthValidator(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    {
        UnitOfWork = unitOfWork;
        LocalizationService = localizationService;
    }

    /// <summary>
    /// Common validation rules that can be applied to any authentication command
    /// Override this method in derived classes to add specific validation rules
    /// </summary>
    protected virtual void SetupValidationRules()
    {
        // Base implementation - override in derived classes
    }
}
