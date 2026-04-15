using PRN232_G9_AutoGradingTool.Infrastructure.Configurations;
using PRN232_G9_AutoGradingTool.Infrastructure.Filters;

namespace PRN232_G9_AutoGradingTool.API.Extensions;

/// <summary>
/// Extension methods for application startup configuration
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Configure application - ĐÃ DỌN DẸP, CHỈ CÒN DATABASE VÀ SERVICE ACCOUNT TEST
    /// </summary>
    public static async Task<WebApplication> ConfigureApplicationAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationStartup");

        try
        {
            if (app.Environment.IsDevelopment())
            {
                // Step 1: Apply main database migrations
                logger.LogInformation("Applying main database migrations...");
                await app.ApplyMigrationsAsync(logger);

                // Step 2: Apply outer database migrations
                logger.LogInformation("Applying outer database migrations...");
                await app.ApplyOuterDbMigrationsAsync(logger);

                // Step 3: Configure Hangfire storage (uses OuterDb)
                logger.LogInformation("Configuring Hangfire storage...");
                await app.Services.ConfigureHangfireStorageAsync(app.Configuration);

                // Step 4: Initialize Hangfire recurring jobs
                logger.LogInformation("Initializing Hangfire jobs...");
                app.Services.UseHangfireConfiguration(app.Configuration);

                // Step 5: Seed initial data
                logger.LogInformation("Seeding initial data...");
                await app.SeedInitialDataAsync(logger);

                logger.LogInformation("Database migrations and seeding completed successfully");
            }
            else
            {
                logger.LogInformation("Skipping database migrations and seeding in production environment");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations.");
            throw;
        }

        logger.LogInformation("Application configuration completed successfully");
        return app;
    }
}
