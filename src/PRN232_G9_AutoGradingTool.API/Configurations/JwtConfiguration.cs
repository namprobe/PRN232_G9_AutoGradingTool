using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace PRN232_G9_AutoGradingTool.API.Configurations;

/// <summary>
/// Extension methods for JWT configuration
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// Configure JWT authentication
    /// </summary>
    public static IServiceCollection AddJwtConfiguration(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        bool requireHttps = false)
    {
        // Get JWT settings
        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (hostEnvironment.IsDevelopment())
        {
            jwtKey ??= "DEV_ONLY_PRN232_JWT_KEY_CHANGE_IN_PRODUCTION_32+";
            jwtIssuer ??= "Autogradingtool";
            audience ??= "AutogradingtoolClients";
        }

        if (string.IsNullOrEmpty(jwtKey))
            throw new ArgumentNullException(nameof(jwtKey), "JWT Key is not configured (set Jwt:Key or Jwt__Key in .env)");

        if (string.IsNullOrEmpty(jwtIssuer))
            throw new ArgumentNullException(nameof(jwtIssuer), "JWT Issuer is not configured");

        if (string.IsNullOrEmpty(audience))
            throw new ArgumentNullException(nameof(audience), "JWT Audience is not configured");

        // Add JWT authentication
        var key = Encoding.UTF8.GetBytes(jwtKey);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = requireHttps;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            // Add event handlers for JWT bearer events
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });
        
        return services;
    }
}