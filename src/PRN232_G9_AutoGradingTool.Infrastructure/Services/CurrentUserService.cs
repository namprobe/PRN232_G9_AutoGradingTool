using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<AppUser> _userManager;
    
    // Cache for validation and roles
    private bool? _cachedValidation;
    private (bool isValid, Guid? userId, IList<RoleEnum> roles)? _cachedValidationWithRoles;
    private (bool isValid, Guid? userId, IList<RoleEnum> roles, AppUser? user)? _cachedValidationWithRolesAndEntity;
    private AppUser? _cachedUser;
    
    // Role claim types
    private static readonly string[] ROLE_CLAIM_TYPES = new[] {
        ClaimTypes.Role, // "http://schemas.microsoft.com/ws/2005/05/identity/claims/role"
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", // Common JWT claim type 
        "role" // Simplified claim type
    };
    
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<AppUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }
    
    /// <summary>
    /// ID of the current user from claims
    /// </summary>
    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    
    /// <summary>
    /// Roles of the current user from JWT claims (quick access - may be outdated)
    /// Note: Use GetCurrentRolesAsync() for up-to-date roles from database
    /// </summary>
    public IEnumerable<RoleEnum> Roles => GetUserRolesFromClaims();
    
    /// <summary>
    /// Validate user existence and status (simplified for login/logout scenarios)
    /// </summary>
    public async Task<(bool isValid, Guid? userId)> IsUserValidAsync()
    {
        // Kiểm tra nếu không có UserId hoặc không thể parse thành Guid
        if (string.IsNullOrEmpty(UserId) || !Guid.TryParse(UserId, out var userGuid))
        {
            return (isValid: false, userId: null);
        }

        // Sử dụng cache để tránh query nhiều lần trong cùng request
        if (_cachedValidation.HasValue)
        {
            return (isValid: _cachedValidation.Value, userId: _cachedValidation.Value ? userGuid : null);
        }

        // Query database để kiểm tra user
        _cachedValidation = await _userManager.IsUserValidAsync(userGuid);
        return (isValid: _cachedValidation.Value, userId: _cachedValidation.Value ? userGuid : null);
    }

    /// <summary>
    /// Get current user roles from database (always up-to-date)
    /// Use this for authorization decisions instead of JWT claims
    /// </summary>
    public async Task<IList<RoleEnum>> GetCurrentRolesAsync()
    {
        var (isValid, _, roles) = await ValidateUserWithRolesAsync();
        return isValid ? roles : new List<RoleEnum>();
    }

    /// <summary>
    /// Validate user and get current roles from database in one call
    /// </summary>
    public async Task<(bool isValid, Guid? userId, IList<RoleEnum> roles)> ValidateUserWithRolesAsync()
    {
        // Kiểm tra nếu không có UserId hoặc không thể parse thành Guid
        if (string.IsNullOrEmpty(UserId) || !Guid.TryParse(UserId, out var userGuid))
        {
            return (isValid: false, userId: null, roles: new List<RoleEnum>());
        }

        // Sử dụng cache để tránh query nhiều lần trong cùng request
        if (_cachedValidationWithRoles.HasValue)
        {
            return _cachedValidationWithRoles.Value;
        }

        // Query database để lấy thông tin user và roles hiện tại
        _cachedValidationWithRoles = await _userManager.ValidateUserWithRolesAsync(userGuid);
        return _cachedValidationWithRoles.Value;
    }

    /// <summary>
    /// Validate user and get current roles and user entity from database in one call
    /// </summary>
    public async Task<(bool isValid, Guid? userId, IList<RoleEnum> roles, AppUser? user)> ValidateUserWithRolesAndEntityAsync()
    {
        // Kiểm tra nếu không có UserId hoặc không thể parse thành Guid
        if (string.IsNullOrEmpty(UserId) || !Guid.TryParse(UserId, out var userGuid))
        {
            return (isValid: false, userId: null, roles: new List<RoleEnum>(), user: null);
        }

        // Sử dụng cache để tránh query nhiều lần trong cùng request
        if (_cachedValidationWithRolesAndEntity.HasValue)
        {
            return _cachedValidationWithRolesAndEntity.Value;
        }

        // Query database để lấy thông tin user, roles và user entity trong một lần
        _cachedValidationWithRolesAndEntity = await _userManager.ValidateUserWithRolesAndEntityAsync(userGuid);
        return _cachedValidationWithRolesAndEntity.Value;
    }

    /// <summary>
    /// Get current user entity from database (cached per request)
    /// Uses UserManager with validation (same logic as ValidateUserWithRolesAsync)
    /// </summary>
    private async Task<AppUser?> GetCurrentUserAsync()
    {
        if (_cachedUser != null)
        {
            return _cachedUser;
        }

        if (string.IsNullOrEmpty(UserId) || !Guid.TryParse(UserId, out var userGuid))
        {
            return null;
        }

        // Use UserManager with same validation logic as ValidateUserWithRolesAsync
        _cachedUser = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userGuid && 
                u.Status == EntityStatusEnum.Active && 
                u.RefreshToken != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow &&
                (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTime.UtcNow));

        return _cachedUser;
    }
    
    /// <summary>
    /// Extract roles from JWT claims and convert to RoleEnum (may be outdated)
    /// </summary>
    private IEnumerable<RoleEnum> GetUserRolesFromClaims()
    {
        if (_httpContextAccessor.HttpContext?.User == null)
        {
            return Enumerable.Empty<RoleEnum>();
        }
        
        var claims = _httpContextAccessor.HttpContext.User.Claims;
        
        // Lấy tất cả roles từ claims và convert sang RoleEnum
        var roles = claims
            .Where(c => ROLE_CLAIM_TYPES.Contains(c.Type))
            .Select(c => c.Value)
            .Where(roleString => Enum.TryParse<RoleEnum>(roleString, true, out _))
            .Select(roleString => Enum.Parse<RoleEnum>(roleString, true))
            .Distinct()
            .ToList();
            
        return roles;
    }
}