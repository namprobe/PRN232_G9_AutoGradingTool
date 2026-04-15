using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;
/// <summary>
/// Implementation of JWT service for generating and validating tokens
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<AppUser> _userManager;

    public JwtService(IOptions<JwtSettings> jwtSettings, UserManager<AppUser> userManager)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
    }

    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    public (string token, List<string> roles) GenerateJwtToken(AppUser user, IList<string> roles)
    {
        // Get user claims and roles

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create token
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), roles.ToList());
    }

    /// <summary>
    /// Generate JWT token with expiration info for a user
    /// </summary>
    public (string token, List<string> roles, int expiresInMinutes, DateTime expiresAt) GenerateJwtTokenWithExpiration(AppUser user, IList<string> roles)
    {
        var (token, roleList) = GenerateJwtToken(user, roles);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);
        return (token, roleList, _jwtSettings.ExpiresInMinutes, expiresAt);
    }

    /// <summary>
    /// Get token expiration time in minutes
    /// </summary>
    public int GetTokenExpirationMinutes()
    {
        return _jwtSettings.ExpiresInMinutes;
    }

    /// <summary>
    /// Generate refresh token
    /// </summary>
    public (string refreshToken, DateTime refreshTokenExpiryTime) GenerateRefreshTokenWithExpiration()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return (Convert.ToBase64String(randomNumber), DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiresInDays));
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get user id from token
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        if (!ValidateToken(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        return jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get principal claims from token
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false // Don't validate lifetime here
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}