using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Exceptions;

namespace PRN232_G9_AutoGradingTool.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRolesAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    
    public AuthorizeRolesAttribute(params string[] roles)
    {
        _roles = roles;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            
        if (allowAnonymous)
            return;
            
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            // Sử dụng UnauthorizedAccessException chuẩn của .NET cho lỗi chưa đăng nhập
            throw new UnauthorizedAccessException();
        }
        
        if (_roles.Length == 0)
            return;
            
        var userRoleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role);
        if (userRoleClaim == null)
        {
            throw new ForbiddenAccessException();
        }
        
        var userRole = userRoleClaim.Value;
        
        if (!_roles.Contains(userRole))
        {
            var details = new List<string> 
            {
                $"Required roles: {string.Join(", ", _roles)}",
                $"Your role: {userRole}"
            };
            throw new InsufficientPermissionsException(details: details);
        }
    }
} 