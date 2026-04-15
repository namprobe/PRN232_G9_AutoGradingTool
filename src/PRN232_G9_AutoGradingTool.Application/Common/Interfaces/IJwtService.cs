using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    /// <param name="user">User for which to generate the token</param>
    /// <param name="roles">Roles of the user</param>
    /// <returns>Token and roles tuple</returns>
    (string token, List<string> roles) GenerateJwtToken(AppUser user, IList<string> roles);
    
    /// <summary>
    /// Generate JWT token with expiration info for a user
    /// </summary>
    /// <param name="user">User for which to generate the token</param>
    /// <returns>Token, roles, expiration time in minutes, and expiration datetime tuple</returns>
    (string token, List<string> roles, int expiresInMinutes, DateTime expiresAt) GenerateJwtTokenWithExpiration(AppUser user, IList<string> roles);
    
    /// <summary>
    /// Get token expiration time in minutes
    /// </summary>
    /// <returns>Token expiration time in minutes</returns>
    int GetTokenExpirationMinutes();
    
    /// <summary>
    /// Generate refresh token for a user
    /// </summary>
    /// <returns>Refresh token string and expiration time</returns>
    (string refreshToken, DateTime refreshTokenExpiryTime) GenerateRefreshTokenWithExpiration();
    
    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Get user id from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID or null if token is invalid</returns>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Get principal claims from token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>ClaimsPrincipal or null if token is invalid</returns>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromToken(string token);
}