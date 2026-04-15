using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class AppUser : IdentityUser<Guid>, IEntityLike
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime JoiningAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }
}