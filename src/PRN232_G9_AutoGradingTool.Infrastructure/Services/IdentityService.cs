using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILocalizationService _localizationService;
    
    public IdentityService(
        UserManager<AppUser> userManager, 
        SignInManager<AppUser> signInManager,
        ILocalizationService localizationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _localizationService = localizationService;
    }
    public async Task<Result<AppUser>> AuthenticateAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.Email);
            if (user == null)
            {
                return Result<AppUser>.Failure(
                    _localizationService.GetErrorMessage(ErrorCodeEnum.InvalidCredentials), 
                    ErrorCodeEnum.InvalidCredentials);
            }

            if (user.Status != EntityStatusEnum.Active)
            {
                return Result<AppUser>.Failure(
                    _localizationService.GetExceptionMessage("Exception_UserNotActive"), 
                    ErrorCodeEnum.InvalidCredentials);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Result<AppUser>.Failure(
                    _localizationService.GetErrorMessage(ErrorCodeEnum.InvalidCredentials), 
                    ErrorCodeEnum.InvalidCredentials);
            }

            // Check if email is confirmed (future feature)
            // if (!user.EmailConfirmed)
            //     return Result<AppUser>.Failure("Email is not confirmed", ErrorCodeEnum.InvalidCredentials);

            return Result<AppUser>.Success(user);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
    {
        try
        {
            IdentityResult result = new();
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var isExists = await IsPhoneNumberDuplicateAsync(user, user.PhoneNumber!);
                if (isExists.IsSuccess && isExists.Data)
                {
                    result = IdentityResult.Failed(new IdentityError 
                    { 
                        Code = "PhoneNumberExists", 
                        Description = _localizationService.GetExceptionMessage("Exception_PhoneNumberExists") 
                    });
                    return result;
                }
            }
            result = await _userManager.CreateAsync(user, password);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IdentityResult> AddUserToRoleAsync(AppUser user, string role)
    {
        try
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(AppUser user)
    {
        try
        {
            var result = await _userManager.GetRolesAsync(user);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<AppUser> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user;
        }
        catch
        {
            throw;
        }
    }

    public async Task<AppUser> GetUserByFirstOrDefaultAsync(Expression<Func<AppUser, bool>> predicate)
    {
        try
        {
            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(predicate);
            return user;
        }
        catch
        {
            throw;
        }
    }

    public async Task<Result<bool>> IsEmailDuplicateAsync(AppUser user, string email)
    {
        try
        {
            var isExists = await _userManager.Users.AnyAsync(x => x.Email == email && x.Id != user.Id);
            if (isExists)
                return Result<bool>.Success(true);
            return Result<bool>.Success(false);
        }
        catch
        {
            throw;
        }
    }

    public async Task<Result<bool>> IsPhoneNumberDuplicateAsync(AppUser user, string phoneNumber)
    {
        try
        {
            var isExists = await _userManager.Users.AnyAsync(x => x.PhoneNumber == phoneNumber && x.Id != user.Id);
            if (isExists)
                return Result<bool>.Success(true);
            return Result<bool>.Success(false);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IdentityResult> RemoveUserRolesAsync(AppUser user, string role)
    {
        try
        {
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IdentityResult> UpdateUserAsync(AppUser user)
    {
        try
        {
            IdentityResult result = new();
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var isExists = await IsPhoneNumberDuplicateAsync(user, user.PhoneNumber!);
                if (isExists.IsSuccess && isExists.Data)
                {
                    result = IdentityResult.Failed(new IdentityError 
                    { 
                        Code = "PhoneNumberExists", 
                        Description = _localizationService.GetExceptionMessage("Exception_PhoneNumberExists") 
                    });
                    return result;
                }
            }
            result = await _userManager.UpdateAsync(user);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<string> GeneratePasswordResetToken(AppUser user)
    {
        try
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IdentityResult> ResetUserPasswordAsync(Expression<Func<AppUser, bool>> contactPredicate, string token, string newPassword)
    {
        try
        {
            // Single query: Find user directly using tracked context
            // This avoids the double-query approach and tracking conflicts
            var user = await _userManager.Users.FirstOrDefaultAsync(contactPredicate);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError 
                { 
                    Code = "UserNotFound", 
                    Description = _localizationService.GetExceptionMessage("Exception_UserNotFound") 
                });
            }

            user.UpdateEntity(user.Id);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

            return resetResult;
        }
        catch
        {
            throw;
        }

    }

    public async Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
    {
        try
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<AppUser?> GetUserByIdIncludeProfileAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user;
        }
        catch
        {
            throw;
        }
    }
}
