using MediatR;
using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using FluentValidation;

namespace PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.UploadAndGradeSubmission;

public class UploadSubmissionAndGradeCommand : IRequest<Result<UploadAndTriggerGradingResponseDto>>
{
    /// <summary>Route parameter — set by Controller.</summary>
    public Guid ExamTopicId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public QuestionLabelEnum QuestionLabel { get; set; }

    public IFormFile ZipFile { get; set; } = null!;
}

public class UploadSubmissionAndGradeCommandValidator : AbstractValidator<UploadSubmissionAndGradeCommand>
{
    public UploadSubmissionAndGradeCommandValidator()
    {
        RuleFor(x => x.ExamTopicId)
            .NotEmpty().WithMessage("ExamTopicId is required.");

        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage("StudentCode is required.");

        RuleFor(x => x.QuestionLabel)
            .IsInEnum().WithMessage("QuestionLabel must be Q1 or Q2.");

        RuleFor(x => x.ZipFile)
            .NotNull().WithMessage("ZipFile is required.")
            .Must(file => file != null && file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file != null && System.IO.Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .zip files are allowed.")
            .Must(file => file == null || file.Length <= 50 * 1024 * 1024)
            .WithMessage("File size exceeds the 50MB limit.");
    }
}
