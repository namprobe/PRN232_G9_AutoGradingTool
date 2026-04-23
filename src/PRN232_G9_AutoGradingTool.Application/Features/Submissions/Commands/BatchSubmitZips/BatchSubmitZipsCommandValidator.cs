using FluentValidation;

namespace PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;

public class BatchSubmitZipsCommandValidator : AbstractValidator<BatchSubmitZipsCommand>
{
    private static readonly string[] ZipExtensions = [".zip"];

    public BatchSubmitZipsCommandValidator()
    {
        RuleFor(x => x.ExamSessionId)
            .NotEmpty().WithMessage("ExamSessionId is required.");

        RuleFor(x => x.Request.Entries)
            .NotEmpty().WithMessage("At least one student entry is required.")
            .Must(e => e.Count <= 50).WithMessage("Cannot submit more than 50 students at once.");

        RuleFor(x => x.Request.Entries)
            .Must(e => e.Select(s => s.StudentCode.Trim().ToUpper()).Distinct().Count() == e.Count)
            .WithMessage("Duplicate student codes are not allowed.")
            .When(x => x.Request.Entries.Count > 0);

        RuleForEach(x => x.Request.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.ExamTopicId)
                .NotEmpty().WithMessage("ExamTopicId is required.");

            entry.RuleFor(e => e.StudentCode)
                .NotEmpty().WithMessage("StudentCode is required.");

            entry.RuleFor(e => e.Q1Zip)
                .NotNull().WithMessage("Q1 zip file is required.")
                .Must(f => f != null && IsZip(f.FileName))
                    .WithMessage("Q1 file must be a .zip archive.");

            entry.RuleFor(e => e.Q2Zip)
                .NotNull().WithMessage("Q2 zip file is required.")
                .Must(f => f != null && IsZip(f.FileName))
                    .WithMessage("Q2 file must be a .zip archive.");
        });
    }

    private static bool IsZip(string fileName)
        => !string.IsNullOrWhiteSpace(fileName) &&
           ZipExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
}
