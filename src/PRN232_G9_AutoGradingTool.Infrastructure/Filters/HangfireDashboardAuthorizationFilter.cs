using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Filters;

/// <summary>
/// Development-friendly authorization filter for Hangfire Dashboard
/// Allows unrestricted access in development mode for testing
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var environment = httpContext.RequestServices.GetService<IHostEnvironment>();
        var logger = httpContext.RequestServices.GetService<ILogger<HangfireDashboardAuthorizationFilter>>();

        // In development mode, allow all access for testing
        if (environment?.IsDevelopment() ?? false)
        {
            logger?.LogDebug("Hangfire dashboard access granted (Development mode - no authentication required)");
            return true;
        }

        // In production, you can implement proper authentication here
        // For now, also allow access for testing purposes
        logger?.LogWarning("Hangfire dashboard accessed in non-development environment without authentication");
        return true; // Change this to implement production authentication

        // Example production authentication (commented out for testing):
        /*
        // Check for JWT token authentication
        var isTokenValid = AuthorizeByJwtToken(httpContext);
        if (isTokenValid)
        {
            logger?.LogInformation("Hangfire dashboard access granted via JWT token");
            return true;
        }

        // Access denied - redirect to login
        logger?.LogWarning("Hangfire dashboard access denied - invalid or missing authentication");
        
        try
        {
            // Check if it's an API request
            var isApiRequest = httpContext.Request.Headers["Accept"]
                .Any(h => h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isApiRequest)
            {
                httpContext.Response.StatusCode = 401;
                return false;
            }

            // For browser requests, redirect to login
            httpContext.Response.Redirect("/login?redirect=hangfire");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling Hangfire authorization redirect");
        }

        return false;
        */
    }

    /// <summary>
    /// Placeholder for JWT token authentication (for future implementation)
    /// </summary>
    private bool AuthorizeByJwtToken(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // TODO: Implement JWT token validation
        // - Get token from cookie or Authorization header
        // - Validate token signature and expiration
        // - Check user roles/permissions
        return false;
    }
}
