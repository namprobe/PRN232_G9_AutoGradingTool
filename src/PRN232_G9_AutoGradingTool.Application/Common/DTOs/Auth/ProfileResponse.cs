using System.Text.Json.Serialization;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;

public class ProfileResponse
{
    
    public string Email { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public List<RoleEnum> Roles { get; set; } = new List<RoleEnum>();
}
