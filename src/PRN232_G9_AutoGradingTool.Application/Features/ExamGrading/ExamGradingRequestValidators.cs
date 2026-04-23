using FluentValidation;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamGrading;

public sealed class EgCreateSemesterCommandValidator : AbstractValidator<EgCreateSemesterCommand>
{
    public EgCreateSemesterCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgUpdateSemesterCommandValidator : AbstractValidator<EgUpdateSemesterCommand>
{
    public EgUpdateSemesterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgDeleteSemesterCommandValidator : AbstractValidator<EgDeleteSemesterCommand>
{
    public EgDeleteSemesterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgCreateExamSessionCommandValidator : AbstractValidator<EgCreateExamSessionCommand>
{
    public EgCreateExamSessionCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.SemesterId).NotEmpty();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body.ExamDurationMinutes).GreaterThan(0);
    }
}

public sealed class EgUpdateExamSessionCommandValidator : AbstractValidator<EgUpdateExamSessionCommand>
{
    public EgUpdateExamSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.SemesterId).NotEmpty();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body.ExamDurationMinutes).GreaterThan(0);
    }
}

public sealed class EgDeleteExamSessionCommandValidator : AbstractValidator<EgDeleteExamSessionCommand>
{
    public EgDeleteExamSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgCreateTopicCommandValidator : AbstractValidator<EgCreateTopicCommand>
{
    public EgCreateTopicCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgUpdateTopicCommandValidator : AbstractValidator<EgUpdateTopicCommand>
{
    public EgUpdateTopicCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgDeleteTopicCommandValidator : AbstractValidator<EgDeleteTopicCommand>
{
    public EgDeleteTopicCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
    }
}

public sealed class EgCreateQuestionCommandValidator : AbstractValidator<EgCreateQuestionCommand>
{
    public EgCreateQuestionCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body.MaxScore).GreaterThan(0);
    }
}

public sealed class EgUpdateQuestionCommandValidator : AbstractValidator<EgUpdateQuestionCommand>
{
    public EgUpdateQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Body.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body.MaxScore).GreaterThan(0);
    }
}

public sealed class EgDeleteQuestionCommandValidator : AbstractValidator<EgDeleteQuestionCommand>
{
    public EgDeleteQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}

public sealed class EgCreateTestCaseCommandValidator : AbstractValidator<EgCreateTestCaseCommand>
{
    public EgCreateTestCaseCommandValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body.MaxPoints).GreaterThan(0);
    }
}

public sealed class EgUpdateTestCaseCommandValidator : AbstractValidator<EgUpdateTestCaseCommand>
{
    public EgUpdateTestCaseCommandValidator()
    {
        RuleFor(x => x.TestCaseId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body.MaxPoints).GreaterThan(0);
    }
}

public sealed class EgDeleteTestCaseCommandValidator : AbstractValidator<EgDeleteTestCaseCommand>
{
    public EgDeleteTestCaseCommandValidator()
    {
        RuleFor(x => x.TestCaseId).NotEmpty();
    }
}

public sealed class EgCreateGradingPackCommandValidator : AbstractValidator<EgCreateGradingPackCommand>
{
    public EgCreateGradingPackCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgUpdateGradingPackCommandValidator : AbstractValidator<EgUpdateGradingPackCommand>
{
    public EgUpdateGradingPackCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().MaximumLength(256);
    }
}

public sealed class EgDeleteGradingPackCommandValidator : AbstractValidator<EgDeleteGradingPackCommand>
{
    public EgDeleteGradingPackCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
    }
}

public sealed class EgCreatePackAssetCommandValidator : AbstractValidator<EgCreatePackAssetCommand>
{
    public EgCreatePackAssetCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.File).NotNull();
    }
}

public sealed class EgDeletePackAssetCommandValidator : AbstractValidator<EgDeletePackAssetCommand>
{
    public EgDeletePackAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class EgGetExamSessionQueryValidator : AbstractValidator<EgGetExamSessionQuery>
{
    public EgGetExamSessionQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgListSubmissionsQueryValidator : AbstractValidator<EgListSubmissionsQuery>
{
    public EgListSubmissionsQueryValidator()
    {
        RuleFor(x => x.ExamSessionId).NotEmpty();
    }
}

public sealed class EgGetSubmissionQueryValidator : AbstractValidator<EgGetSubmissionQuery>
{
    public EgGetSubmissionQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgCreateSubmissionCommandValidator : AbstractValidator<EgCreateSubmissionCommand>
{
    public EgCreateSubmissionCommandValidator()
    {
        RuleFor(x => x.ExamSessionId).NotEmpty();
        RuleFor(x => x.StudentCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Q1Zip).NotNull();
        RuleFor(x => x.Q2Zip).NotNull();
    }
}

public sealed class EgListExamClassesQueryValidator : AbstractValidator<EgListExamClassesQuery>
{
    public EgListExamClassesQueryValidator()
    {
        RuleFor(x => x.SemesterId).NotEmpty();
    }
}

public sealed class EgCreateExamClassCommandValidator : AbstractValidator<EgCreateExamClassCommand>
{
    public EgCreateExamClassCommandValidator()
    {
        RuleFor(x => x.SemesterId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body.MaxStudents).GreaterThan(0);
    }
}

public sealed class EgUpdateExamClassCommandValidator : AbstractValidator<EgUpdateExamClassCommand>
{
    public EgUpdateExamClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body.MaxStudents).GreaterThan(0);
    }
}

public sealed class EgDeleteExamClassCommandValidator : AbstractValidator<EgDeleteExamClassCommand>
{
    public EgDeleteExamClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgListExamSessionClassesQueryValidator : AbstractValidator<EgListExamSessionClassesQuery>
{
    public EgListExamSessionClassesQueryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class EgCreateExamSessionClassCommandValidator : AbstractValidator<EgCreateExamSessionClassCommand>
{
    public EgCreateExamSessionClassCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.ExamClassId).NotEmpty();
        RuleFor(x => x.Body.ExpectedStudentCount).GreaterThan(0);
    }
}

public sealed class EgDeleteExamSessionClassCommandValidator : AbstractValidator<EgDeleteExamSessionClassCommand>
{
    public EgDeleteExamSessionClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EgStartClassBatchGradingCommandValidator : AbstractValidator<EgStartClassBatchGradingCommand>
{
    public EgStartClassBatchGradingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotNull();
    }
}

public sealed class EgReplaceSubmissionFileCommandValidator : AbstractValidator<EgReplaceSubmissionFileCommand>
{
    public EgReplaceSubmissionFileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.QuestionLabel).NotEmpty().MaximumLength(16);
        RuleFor(x => x.ZipFile).NotNull();
    }
}

public sealed class EgTriggerRegradeCommandValidator : AbstractValidator<EgTriggerRegradeCommand>
{
    public EgTriggerRegradeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
