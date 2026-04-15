namespace PRN232_G9_AutoGradingTool.Application.Common.Models;

public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
    
    /// <summary>
    /// Token audience(s), comma separated
    /// </summary>
    public string? Audience { get; set; }
    
    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 60;
    
    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpiresInDays { get; set; } = 7;
} 