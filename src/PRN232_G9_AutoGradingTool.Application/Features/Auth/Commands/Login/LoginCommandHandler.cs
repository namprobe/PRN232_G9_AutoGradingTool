using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly ILocalizationService _localizationService;

    public LoginCommandHandler(
        IIdentityService identityService, 
        IJwtService jwtService, 
        ILogger<LoginCommandHandler> logger,
        ILocalizationService localizationService)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _logger = logger;
        _localizationService = localizationService;
    }
    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identityService.AuthenticateAsync(command.Request);
            if (!result.IsSuccess)
            {
                return Result<AuthResponse>.Failure(
                result.Message ?? _localizationService.GetErrorMessage(ErrorCodeEnum.InvalidCredentials), 
                ErrorCodeEnum.InvalidCredentials);
            }

            var user = result.Data!;
            //generate refresh token and update auth infor of user
            var (refreshToken, refreshTokenExpiryTime) = _jwtService.GenerateRefreshTokenWithExpiration();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdateEntity(user.Id);
            await _identityService.UpdateUserAsync(user);
            var userRoles = await _identityService.GetUserRolesAsync(user);
            if (!userRoles.Any())
            {
                return Result<AuthResponse>.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }
            //generate jwt token
            var (token, roles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user, userRoles);
            return Result<AuthResponse>.Success(new AuthResponse
            {
                AccessToken = token,
                Roles = roles.Select(x => Enum.Parse<RoleEnum>(x)).ToList(),
                ExpiresAt = expiresAt
            }, _localizationService.GetSuccessMessage("Success_Login"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in");
            return Result<AuthResponse>.Failure(
                _localizationService.GetErrorMessage(ErrorCodeEnum.InternalError), 
                ErrorCodeEnum.InternalError);
        }
    }
}