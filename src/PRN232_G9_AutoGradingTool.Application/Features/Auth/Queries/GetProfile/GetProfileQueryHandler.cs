using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    private readonly ILogger<GetProfileQueryHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizationService;

    public GetProfileQueryHandler(
        ILogger<GetProfileQueryHandler> logger, 
        IMapper mapper, 
        IIdentityService identityService, 
        ICurrentUserService currentUserService,
        ILocalizationService localizationService)
    {
        _logger = logger;
        _mapper = mapper;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _localizationService = localizationService;
    }

    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (isValid, userId, roles, user) = await _currentUserService.ValidateUserWithRolesAndEntityAsync();
            if (!isValid || userId == null || user == null)
            {
                return Result<ProfileResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }

            var userRoles = await _identityService.GetUserRolesAsync(user);
            var profileResponse = _mapper.Map<ProfileResponse>(user);
            if (userRoles.Any())
            {
                profileResponse.Roles = userRoles.Select(x => Enum.Parse<RoleEnum>(x)).ToList();
            }
            return Result<ProfileResponse>.Success(profileResponse, _localizationService.GetSuccessMessage("Success_ProfileRetrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting profile for user {UserId}", _currentUserService.UserId);
            return Result<ProfileResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.InternalError), ErrorCodeEnum.InternalError);
        }
    }
}