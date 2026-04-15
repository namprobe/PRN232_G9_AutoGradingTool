using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.User;

public class UpdateUserRequest
{
    public string? FirstName { get; set; } 
    public string? LastName { get; set; } 
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public EntityStatusEnum Status { get; set; }
    
    // Optional: Update role
    public RoleEnum? NewRole { get; set; } 
}
    
    
