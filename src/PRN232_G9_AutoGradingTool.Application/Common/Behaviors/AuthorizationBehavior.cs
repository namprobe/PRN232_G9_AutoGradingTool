using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using PRN232_G9_AutoGradingTool.Application.Common.Exceptions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior to enforce authorization on requests
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    
    public AuthorizationBehavior(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }
    
    /// <summary>
    /// Handle the authorization pipeline
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();
        
        if (!authorizeAttributes.Any())
        {
            // No authorization required
            return await next();
        }
        
        // Authorization required
        if (string.IsNullOrEmpty(_currentUserService.UserId))
        {
            throw new UnauthorizedAccessException();
        }
        
        // Role-based authorization
        var authorizeAttributesWithRoles = authorizeAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));
        
        if (authorizeAttributesWithRoles.Any())
        {
            var userRoles = _currentUserService.Roles.Select(r => r.ToString());
            
            var authorized = authorizeAttributesWithRoles.All(a => 
                a.Roles.Split(',').Any(r => userRoles.Contains(r.Trim())));
                
            if (!authorized)
            {
                throw new ForbiddenAccessException();
            }
        }
        
        // Continue with the pipeline
        return await next();
    }
}