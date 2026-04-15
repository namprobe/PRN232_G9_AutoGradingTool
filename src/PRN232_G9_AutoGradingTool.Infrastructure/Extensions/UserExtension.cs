using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Extensions;

/// <summary>
/// User extension methods for authentication/authorization (simplified for login/logout scenarios)
/// </summary>
public static class UserExtension
{
    /// <summary>
    /// Check if user is valid and active (for login/logout validation)
    /// </summary>
    /// <param name="userManager">User Manager</param>
    /// <param name="userId">User ID from JWT claim</param>
    /// <returns>True if user is valid, false otherwise</returns>
    public static async Task<bool> IsUserValidAsync(
        this UserManager<AppUser> userManager,
        Guid? userId)
    {
        if (userId == null)
        {
            return false;
        }

        return await userManager.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == userId && 
                u.Status == EntityStatusEnum.Active && 
                u.RefreshToken != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow &&
                (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTime.UtcNow));
    }

    /// <summary>
    /// Validate user and get current roles from database (always up-to-date)
    /// </summary>
    /// <param name="userManager">User Manager</param>
    /// <param name="userId">User ID from JWT claim</param>
    /// <returns>Tuple with isValid, userId, and current roles from database</returns>
    public static async Task<(bool isValid, Guid? userId, IList<RoleEnum> roles)> ValidateUserWithRolesAsync(
        this UserManager<AppUser> userManager,
        Guid? userId)
    {
        if (userId == null)
        {
            return (false, null, new List<RoleEnum>());
        }

        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId && 
                u.Status == EntityStatusEnum.Active && 
                u.RefreshToken != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow &&
                (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTime.UtcNow));

        if (user == null)
        {
            return (false, null, new List<RoleEnum>());
        }

        // Lấy roles hiện tại từ database (luôn up-to-date)
        var userRoleStrings = await userManager.GetRolesAsync(user);
        var userRoles = userRoleStrings
            .Where(roleString => Enum.TryParse<RoleEnum>(roleString, true, out _))
            .Select(roleString => Enum.Parse<RoleEnum>(roleString, true))
            .Distinct()
            .ToList();

        return (true, userId, userRoles);
    }

    /// <summary>
    /// Validate user and get current roles and user entity from database in one call
    /// </summary>
    /// <param name="userManager">User Manager</param>
    /// <param name="userId">User ID from JWT claim</param>
    /// <returns>Tuple with isValid, userId, current roles, and user entity from database</returns>
    public static async Task<(bool isValid, Guid? userId, IList<RoleEnum> roles, AppUser? user)> ValidateUserWithRolesAndEntityAsync(
        this UserManager<AppUser> userManager,
        Guid? userId)
    {
        if (userId == null)
        {
            return (false, null, new List<RoleEnum>(), null);
        }

        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId && 
                u.Status == EntityStatusEnum.Active && 
                u.RefreshToken != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow &&
                (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTime.UtcNow));

        if (user == null)
        {
            return (false, null, new List<RoleEnum>(), null);
        }

        // Lấy roles hiện tại từ database (luôn up-to-date)
        var userRoleStrings = await userManager.GetRolesAsync(user);
        var userRoles = userRoleStrings
            .Where(roleString => Enum.TryParse<RoleEnum>(roleString, true, out _))
            .Select(roleString => Enum.Parse<RoleEnum>(roleString, true))
            .Distinct()
            .ToList();

        return (true, userId, userRoles, user);
    }
}