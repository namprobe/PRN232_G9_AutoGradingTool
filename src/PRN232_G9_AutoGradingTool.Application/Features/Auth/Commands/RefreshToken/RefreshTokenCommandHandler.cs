using MediatR;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly IJwtService _jwtService;
    private readonly ILocalizationService _localizationService;

    public RefreshTokenCommandHandler(
        ICurrentUserService currentUserService, 
        IIdentityService identityService, 
        ILogger<RefreshTokenCommandHandler> logger, 
        IJwtService jwtService,
        ILocalizationService localizationService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
        _jwtService = jwtService;
        _localizationService = localizationService;
    }
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var (isValid, userId) = await _currentUserService.IsUserValidAsync();
            if (!isValid || userId == null)
            {
                return Result<AuthResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }

            var user = await _identityService.GetUserByIdAsync(userId.ToString() ?? throw new InvalidOperationException("User ID is null"));
            if (user == null)
            {
                return Result<AuthResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.NotFound), ErrorCodeEnum.NotFound);
            }
             var userRoles = await _identityService.GetUserRolesAsync(user);
            if (!userRoles.Any())
            {
                return Result<AuthResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }
            var (token, roles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user, userRoles);
            var authResponse = new AuthResponse
            {
                AccessToken = token,
                Roles = roles.Select(x => Enum.Parse<RoleEnum>(x)).ToList(),
                ExpiresAt = expiresAt
            };
            return Result<AuthResponse>.Success(authResponse, _localizationService.GetSuccessMessage("Success_RefreshToken"));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", _currentUserService.UserId);
            return Result<AuthResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.InternalError), ErrorCodeEnum.InternalError);
        }
    }
}