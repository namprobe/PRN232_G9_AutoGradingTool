using FluentValidation;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Validators;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : BaseAuthValidator<LoginCommand>
{
    public LoginCommandValidator(IUnitOfWork unitOfWork, ILocalizationService localizationService) 
        : base(unitOfWork, localizationService)
    {
        SetupValidationRules();
    }

    protected override void SetupValidationRules()
    {
        // ValidEmail() already includes NotEmpty() check, so we don't need separate NotEmpty() rule
        RuleFor(x => x.Request.Email)
            .ValidEmail(LocalizationService);
        
        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .WithMessage(LocalizationService.GetValidationMessage("Password_Required"));
    }
}