using MediatR;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<LogoutCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizationService;

    public LogoutCommandHandler(
        IIdentityService identityService, 
        ILogger<LogoutCommandHandler> logger, 
        ICurrentUserService currentUserService,
        ILocalizationService localizationService)
    {
        _identityService = identityService;
        _logger = logger;
        _currentUserService = currentUserService;
        _localizationService = localizationService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }
            var result = await _identityService.GetUserByIdAsync(userId);
            if (result == null)
            {
                return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.NotFound), ErrorCodeEnum.NotFound);
            }
            var user = result;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdateEntity(Guid.Parse(userId));
            await _identityService.UpdateUserAsync(user);
            return Result.Success(_localizationService.GetSuccessMessage("Success_Logout"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out");
            return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.InternalError), ErrorCodeEnum.InternalError);
        }
    }
}