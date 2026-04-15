using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Exceptions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Middleware for validating JWT tokens in requests
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    
    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    /// <summary>
    /// Process the request to validate JWT token
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var endpoint = context.GetEndpoint();
        bool requiresAuthorization = false;
        
        if (endpoint != null)
        {
            requiresAuthorization = endpoint.Metadata.GetMetadata<AuthorizeAttribute>() != null && 
                                  endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() == null;
        }
        
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        
        if (token != null)
        {
            var userAttached = AttachUserToContext(context, jwtService, token);
            
            if (!userAttached && requiresAuthorization)
            {
                // Token không hợp lệ hoặc đã hết hạn
                throw new InvalidTokenException();
            }
        }
        else if (requiresAuthorization)
        {
            // Không có token nhưng endpoint yêu cầu xác thực
            throw new UnauthorizedAccessException();
        }
            
        await _next(context);
    }
    
    /// <summary>
    /// Attach user information to the HttpContext if token is valid
    /// </summary>
    private bool AttachUserToContext(HttpContext context, IJwtService jwtService, string token)
    {
        try
        {
            if (jwtService.ValidateToken(token))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var claims = jwtToken.Claims.ToList();
                
                var userId = claims.FirstOrDefault(x => 
                    x.Type == ClaimTypes.NameIdentifier || 
                    x.Type == "sub" || 
                    x.Type == "nameid")?.Value;
                    
                if (!string.IsNullOrEmpty(userId))
                {
                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
}

public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}