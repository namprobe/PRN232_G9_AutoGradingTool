using PRN232_G9_AutoGradingTool.API.Injection;
using PRN232_G9_AutoGradingTool.Application;
using PRN232_G9_AutoGradingTool.Infrastructure;
using PRN232_G9_AutoGradingTool.Infrastructure.Configurations;
using PRN232_G9_AutoGradingTool.Infrastructure.Filters;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace PRN232_G9_AutoGradingTool.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Register all application services and infrastructure to keep Program.cs minimal
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Core ASP.NET services
        builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
    {
        // Disable automatic 400 response for model validation errors
        options.SuppressModelStateInvalidFilter = true;
    });


        builder.Services.AddHealthChecks();

        builder.Services.AddEndpointsApiExplorer();        // Custom Swagger configuration with tagging and styling
        builder.Services.AddSwaggerConfiguration();

        // Cross-cutting concerns
        builder.AddLoggingConfiguration();

        // Security configurations
        builder.Services.AddJwtConfiguration(builder.Configuration, builder.Environment);
        builder.Services.AddCorsConfiguration(builder.Configuration);

        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // API layer services (filters, middlewares, validation, etc.)
        builder.Services.AddApiServices(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Configure Request Localization (MUST be early in pipeline, before routing)
        // Read supported cultures from configuration (comma-separated string or array)
        var supportedCulturesString = app.Configuration.GetValue<string>("Localization:SupportedCultures");
        var supportedCultures = !string.IsNullOrWhiteSpace(supportedCulturesString)
            ? supportedCulturesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToArray()
            : new[] { "en", "vi" }; // Default fallback
        
        // Read default culture from configuration
        var defaultCulture = app.Configuration.GetValue<string>("Localization:DefaultCulture") ?? "en";
        
        // Log localization configuration for debugging
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("LocalizationConfiguration");
        logger.LogInformation("=== LOCALIZATION CONFIGURATION ===");
        logger.LogInformation("📝 Raw config value (Localization:SupportedCultures): '{SupportedCulturesString}'", 
            supportedCulturesString ?? "(null or empty)");
        logger.LogInformation("🌍 Supported Cultures: [{SupportedCultures}]", string.Join(", ", supportedCultures));
        logger.LogInformation("📝 Raw config value (Localization:DefaultCulture): '{DefaultCultureString}'", 
            app.Configuration.GetValue<string>("Localization:DefaultCulture") ?? "(null or empty)");
        logger.LogInformation("🔤 Default Culture: {DefaultCulture}", defaultCulture);
        logger.LogInformation("====================================");
        
        var localizationOptions = new Microsoft.AspNetCore.Builder.RequestLocalizationOptions()
            .SetDefaultCulture(defaultCulture)
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        // Priority: Accept-Language header only
        localizationOptions.RequestCultureProviders = new List<Microsoft.AspNetCore.Localization.IRequestCultureProvider>
        {
            new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider()
        };

        app.UseRequestLocalization(localizationOptions);
        
        if (app.Environment.IsDevelopment())
        {
            // Use custom Swagger configuration with styling and tagging
            app.UseSwaggerConfiguration(app.Environment);

            // Hangfire Dashboard (Development only - no auth required)
            // Only enable if Hangfire is configured AND services are registered
            var useHangfire = app.Configuration.GetValue("Hangfire:UseOuterDatabase", false);
            var outerConnectionString = app.Configuration.GetConnectionString("OuterDbConnection");
            
            // Only enable dashboard if Hangfire is enabled AND connection string exists
            // (same conditions as in InfrastructureDependencyInjection)
            if (useHangfire && !string.IsNullOrEmpty(outerConnectionString))
            {
                try
                {
                    app.UseHangfireDashboard("/hangfire", new DashboardOptions
                    {
                        Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                        StatsPollingInterval = 2000,
                        DisplayStorageConnectionString = false
                    });
                }
                catch (InvalidOperationException)
                {
                    // Hangfire services not registered, skip dashboard silently
                    // This can happen if AddHangfireServices wasn't called
                }
            }
        }

        // Enable CORS FIRST - Must be before UseHttpsRedirection to avoid preflight redirect issues
        app.UseCorsConfiguration();

        // Enable static files for Swagger custom CSS
        app.UseStaticFiles();

        // HTTPS Redirection - Only in production or when HTTPS is configured
        // In development, this can cause CORS preflight issues
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // API-specific middlewares (exception handling, JWT, etc.)
        app.UseApiConfiguration();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
