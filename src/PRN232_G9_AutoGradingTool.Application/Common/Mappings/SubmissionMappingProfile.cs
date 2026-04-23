using AutoMapper;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Helpers;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Mappings;

public class SubmissionMappingProfile : Profile
{
    public SubmissionMappingProfile()
    {
        // StudentZipEntry → ExamSubmission (skeleton; ExamSessionId/PackId set in handler)
        CreateMap<StudentZipEntry, ExamSubmission>()
            .IgnoreAllBaseEntityFields()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode.Trim()))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.StudentName))
            .ForMember(dest => dest.WorkflowStatus, opt => opt.MapFrom(_ => ExamSubmissionStatus.Pending))
            .ForMember(dest => dest.ExamSessionId, opt => opt.Ignore())
            .ForMember(dest => dest.ExamSessionClassId, opt => opt.Ignore())
            .ForMember(dest => dest.ExamGradingPackId, opt => opt.Ignore())
            .ForMember(dest => dest.TotalScore, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Result, opt => opt.Ignore())
            .ForMember(dest => dest.SubmissionFiles, opt => opt.Ignore())
            .ForMember(dest => dest.QuestionScores, opt => opt.Ignore())
            .ForMember(dest => dest.TestCaseScores, opt => opt.Ignore())
            .ForMember(dest => dest.GradingJobs, opt => opt.Ignore())
            .ForMember(dest => dest.ExamSessionClass, opt => opt.Ignore())
            .ForMember(dest => dest.ExamSession, opt => opt.Ignore())
            .ForMember(dest => dest.ExamGradingPack, opt => opt.Ignore());

        // ExamSubmission → TestResult (seed; SubmissionId set after mapping)
        CreateMap<ExamSubmission, TestResult>()
            .IgnoreAllBaseEntityFields()
            .ForMember(dest => dest.SubmissionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TotalScore, opt => opt.MapFrom(_ => 0.0))
            .ForMember(dest => dest.TestStatus, opt => opt.MapFrom(_ => ExamTestCaseOutcome.Pending))
            .ForMember(dest => dest.Submission, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.Ignore());
    }
}
