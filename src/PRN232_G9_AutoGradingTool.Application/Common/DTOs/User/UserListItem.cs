using PRN232_G9_AutoGradingTool.Application.Common.DTOs;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;

public class UserListItem : BaseResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime JoiningAt { get; set; }
    public List<RoleEnum> Roles { get; set; } = new List<RoleEnum>();
}
