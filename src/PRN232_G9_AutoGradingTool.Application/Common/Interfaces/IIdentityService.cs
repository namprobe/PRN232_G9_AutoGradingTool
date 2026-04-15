
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<AppUser>> AuthenticateAsync(LoginRequest request);
    Task<IdentityResult> CreateUserAsync(AppUser user, string password);
    Task<IdentityResult> AddUserToRoleAsync(AppUser user, string role);
    Task<IList<string>> GetUserRolesAsync(AppUser user);
    Task<AppUser> GetUserByIdAsync(string userId);
    Task<AppUser> GetUserByFirstOrDefaultAsync(Expression<Func<AppUser, bool>> predicate);
    Task<Result<bool>> IsEmailDuplicateAsync(AppUser user, string email);
    Task<Result<bool>> IsPhoneNumberDuplicateAsync(AppUser user, string phoneNumber);
    Task<IdentityResult> UpdateUserAsync(AppUser user);
    Task<IdentityResult> RemoveUserRolesAsync(AppUser user, string role);
    Task<IdentityResult> ResetUserPasswordAsync(Expression<Func<AppUser, bool>> contactPredicate, string token, string newPassword);
    Task<string> GeneratePasswordResetToken(AppUser user);
    Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
    Task<AppUser?> GetUserByIdIncludeProfileAsync(Guid userId);

}