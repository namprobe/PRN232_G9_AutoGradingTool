using MediatR;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizationService;
    
    public ChangePasswordCommandHandler(
        IIdentityService identityService, 
        ILogger<ChangePasswordCommandHandler> logger, 
        ICurrentUserService currentUserService,
        ILocalizationService localizationService)
    {
        _identityService = identityService;
        _logger = logger;
        _currentUserService = currentUserService;
        _localizationService = localizationService;
    }
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var (isValid, userId) = await _currentUserService.IsUserValidAsync();
            if (!isValid || userId == null)
            {
                return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), ErrorCodeEnum.Unauthorized);
            }
            var user = await _identityService.GetUserByIdAsync(userId.Value.ToString());
            user.UpdateEntity(userId);
            var result = await _identityService.ChangePasswordAsync(user, request.Request.CurrentPassword, request.Request.NewPassword);
            if (!result.Succeeded && result.Errors.Any(x => x.Code == "PasswordMismatch"))
            {
                return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.InvalidCredentials), ErrorCodeEnum.InvalidCredentials);
            }
            if (!result.Succeeded)
            {
                var errorMessage = result.Errors.Any() 
                    ? string.Join(" ", result.Errors.Select(x => x.Description))
                    : _localizationService.GetErrorMessage(ErrorCodeEnum.InternalError);
                return Result.Failure(errorMessage, ErrorCodeEnum.InternalError);
            }
            return Result.Success(_localizationService.GetSuccessMessage("Success_PasswordChanged"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return Result.Failure(_localizationService.GetErrorMessage(ErrorCodeEnum.InternalError), ErrorCodeEnum.InternalError);
        }
    }
}