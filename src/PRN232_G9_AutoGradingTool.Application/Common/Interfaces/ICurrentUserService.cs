using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Service to get information about the current user (simplified for login/logout scenarios)
/// </summary>

public interface ICurrentUserService
{
    /// <summary>
    /// ID of the current user from JWT claims
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Roles of the current user from JWT claims (for quick access - may be outdated)
    /// Note: Use GetCurrentRolesAsync() for up-to-date roles from database
    /// </summary>
    IEnumerable<RoleEnum> Roles { get; }

    /// <summary>
    /// Validate user existence and status (for register/logout scenarios)
    /// </summary>
    /// <returns>Tuple with isValid and userId (null if invalid)</returns>
    Task<(bool isValid, Guid? userId)> IsUserValidAsync();

    /// <summary>
    /// Get current user roles from database (always up-to-date)
    /// Use this for authorization decisions instead of JWT claims
    /// </summary>
    /// <returns>Current roles from database, empty if user invalid</returns>
    Task<IList<RoleEnum>> GetCurrentRolesAsync();

    /// <summary>
    /// Validate user and get current roles from database in one call
    /// </summary>
    /// <returns>Tuple with isValid, userId, and current roles from database</returns>
    Task<(bool isValid, Guid? userId, IList<RoleEnum> roles)> ValidateUserWithRolesAsync();

    /// <summary>
    /// Validate user and get current roles and user entity from database in one call
    /// </summary>
    /// <returns>Tuple with isValid, userId, current roles, and user entity from database</returns>
    Task<(bool isValid, Guid? userId, IList<RoleEnum> roles, Domain.Entities.AppUser? user)> ValidateUserWithRolesAndEntityAsync();
}
