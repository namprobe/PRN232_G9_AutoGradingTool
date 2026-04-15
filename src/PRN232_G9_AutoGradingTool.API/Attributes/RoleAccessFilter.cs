using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Exceptions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.API.Attributes;

/// <summary>
/// Base filter for system access control
/// </summary>
public abstract class SystemAccessFilterBase : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Method == "OPTIONS")
            return;
            
        if (context.HttpContext.Request.Method == "POST" && 
            context.HttpContext.Request.HasJsonContentType())
        {
            context.HttpContext.Items["ProcessLoginResult"] = true;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items["ProcessLoginResult"] == null)
            return;
            
        if (context.Result is not ObjectResult objectResult || objectResult.StatusCode != 200)
            return;
        
        if (objectResult.Value is not Result<AuthResponse> authResult || !authResult.IsSuccess)
            return;
            
        var authResponse = authResult.Data;
        
        if (authResponse != null && !IsAuthorizedForSystem(authResponse))
        {
            var systemName = GetSystemName();
            var allowedRoles = GetAllowedRolesDescription();
            
            var details = new List<string> 
            { 
                $"This area is only accessible to {allowedRoles}. Please use an appropriate account."
            };
            throw new InsufficientPermissionsException(
                $"You do not have access to the {systemName}.",
                ErrorCodeEnum.InsufficientPermissions,
                details);
        }
    }
    
    protected abstract bool IsAuthorizedForSystem(AuthResponse user);
    protected abstract string GetSystemName();
    protected abstract string GetAllowedRolesDescription();
    
    /// <summary>
    /// Check if the user has a specific role based on the AppRole list
    /// </summary>
    protected bool HasRole(AuthResponse user, string role)
    {
        if (user.Roles == null || string.IsNullOrEmpty(role))
            return false;
            
        // Try to parse the role string to RoleEnum and check if it exists in the list
        if (Enum.TryParse<RoleEnum>(role, true, out var roleEnum))
        {
            return user.Roles.Contains(roleEnum);
        }
        
        return false;
    }
}


/// <summary>
/// Filter that allows only Staff and admin access
/// </summary>
public class StaffRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(AuthResponse user)
    {
        return HasRole(user, RoleEnum.Instructor.ToString()) || HasRole(user, RoleEnum.SystemAdmin.ToString())|| HasRole(user, RoleEnum.Instructor.ToString());
    }
    
    protected override string GetSystemName()
    {
        return "Store Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return $"{RoleEnum.Instructor.ToString()}, {RoleEnum.SystemAdmin.ToString()} and {RoleEnum.Instructor.ToString()}";
    }
}

/// <summary>
/// Filter that allows only admin and Staff access to admin portal
/// </summary>
public class AdminRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(AuthResponse user)
    {
        return HasRole(user, RoleEnum.Instructor.ToString()) || HasRole(user, RoleEnum.SystemAdmin.ToString())|| HasRole(user, RoleEnum.Instructor.ToString());
    }
    
    protected override string GetSystemName()
    {
        return "Admin Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return $"{RoleEnum.Instructor.ToString()}, {RoleEnum.SystemAdmin.ToString()} and {RoleEnum.Instructor.ToString()}";
    }
} 