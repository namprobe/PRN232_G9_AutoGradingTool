using FluentValidation;
using PRN232_G9_AutoGradingTool.Application.Common.Validators;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Commons.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .ValidPersonName("First name", 100);

        RuleFor(x => x.LastName)
            .ValidPersonName("Last name", 100);

        RuleFor(x => x.Email)
            .ValidEmail()
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Password)
            .ValidPassword(6);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role. Must be Staff or Admin");

        When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .ValidPhoneNumber();
        });
    }
}
