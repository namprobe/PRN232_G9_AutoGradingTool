using System.Text.Json.Serialization;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;

public class UserResponse : UserListItem
{
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
}
