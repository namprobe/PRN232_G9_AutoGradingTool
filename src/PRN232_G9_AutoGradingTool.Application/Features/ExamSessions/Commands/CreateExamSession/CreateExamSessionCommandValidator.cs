using FluentValidation;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamSessions.Commands.CreateExamSession;

public class CreateExamSessionCommandValidator : AbstractValidator<CreateExamSessionCommand>
{
    public CreateExamSessionCommandValidator()
    {
        RuleFor(x => x.SemesterId)
            .NotEmpty().WithMessage("SemesterId bắt buộc.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code bắt buộc.")
            .MaximumLength(50).WithMessage("Code không được vượt quá 50 ký tự.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title bắt buộc.")
            .MaximumLength(200).WithMessage("Title không được vượt quá 200 ký tự.");

        RuleFor(x => x.ExamDurationMinutes)
            .GreaterThan(0).WithMessage("examDurationMinutes phải > 0.");

        RuleFor(x => x.EndsAtUtc)
            .GreaterThan(x => x.StartsAtUtc).WithMessage("EndsAtUtc phải sau StartsAtUtc.");

        RuleForEach(x => x.Topics).ChildRules(topic =>
        {
            topic.RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Topic title bắt buộc.")
                .MaximumLength(200).WithMessage("Topic title không được vượt quá 200 ký tự.");

            topic.RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Topic sortOrder phải >= 0.");
        });
    }
}
