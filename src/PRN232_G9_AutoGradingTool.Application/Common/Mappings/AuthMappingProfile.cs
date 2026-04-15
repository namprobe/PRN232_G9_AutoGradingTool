using AutoMapper;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Helpers;
using PRN232_G9_AutoGradingTool.Application.Common.Mappings.Resolvers;
using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Application.Common.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<AppUser, ProfileResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AvatarPath, 
                opt => opt.ConvertUsing<FilePathUrlConverter, string?>(src => src.AvatarPath));
    }
}