using System.Text.Json.Serialization;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}