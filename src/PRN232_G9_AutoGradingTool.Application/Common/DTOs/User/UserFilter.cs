using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;

public class UserFilter : BasePaginationFilter
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public RoleEnum? Role { get; set; }
    public DateTime? JoiningFrom { get; set; }
    public DateTime? JoiningTo { get; set; }
}
