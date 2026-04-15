using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.API.Extensions;

public static class MigrationExtension
{
    /// <summary>
    /// Tự động apply các migration pending cho PRN232_G9_AutoGradingTool database
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    /// <param name="logger">ILogger</param>
    /// <returns>Task</returns>
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var PRN232_G9_AutoGradingToolContext = scope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>();

            logger.LogInformation("Starting PRN232_G9_AutoGradingTool database migrations...");

            // Kiểm tra kết nối database với retry logic
            try
            {
                await RetryDatabaseConnectionAsync(PRN232_G9_AutoGradingToolContext, "PRN232_G9_AutoGradingTool", logger);
                logger.LogInformation("Successfully connected to PRN232_G9_AutoGradingTool database.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to PRN232_G9_AutoGradingTool database after multiple attempts!");
                throw;
            }

            // Apply pending migrations cho PRN232_G9_AutoGradingTool
            try
            {
                var pendingMigrations = await PRN232_G9_AutoGradingToolContext.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await PRN232_G9_AutoGradingToolContext.Database.GetAppliedMigrationsAsync();

                logger.LogInformation(
                    "PRN232_G9_AutoGradingTool DB: Found {PendingCount} pending migrations and {AppliedCount} previously applied migrations",
                    pendingMigrations.Count(),
                    appliedMigrations.Count());

                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying pending PRN232_G9_AutoGradingTool migrations: {Migrations}",
                        string.Join(", ", pendingMigrations));

                    await PRN232_G9_AutoGradingToolContext.Database.MigrateAsync();
                    logger.LogInformation("Successfully applied all pending PRN232_G9_AutoGradingTool migrations.");
                }
                else
                {
                    logger.LogInformation("No pending migrations found for PRN232_G9_AutoGradingTool database.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying PRN232_G9_AutoGradingTool migrations!");
                throw;
            }

            logger.LogInformation("PRN232_G9_AutoGradingTool database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A problem occurred during PRN232_G9_AutoGradingTool database migrations!");
            throw;
        }
    }

    /// <summary>
    /// Thử kết nối database với retry logic
    /// </summary>
    private static async Task RetryDatabaseConnectionAsync(DbContext context, string contextName,
        ILogger logger, int maxRetries = 3, int delaySeconds = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to connect to {ContextName} database (attempt {Attempt}/{MaxRetries})...",
                    contextName, attempt, maxRetries);

                // Test connection
                await context.Database.CanConnectAsync();
                logger.LogInformation("Successfully connected to {ContextName} database on attempt {Attempt}",
                    contextName, attempt);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex,
                    "Failed to connect to {ContextName} database on attempt {Attempt}. Retrying in {DelaySeconds} seconds...",
                    contextName, attempt, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to {ContextName} database after {MaxRetries} attempts",
                    contextName, maxRetries);
                throw;
            }
        }
    }

    /// <summary>
    /// Đảm bảo database được tạo nếu chưa tồn tại
    /// </summary>
    public static void EnsureDatabaseCreated(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var PRN232_G9_AutoGradingToolDbContext = scope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>();

            logger.LogInformation("Checking if PRN232_G9_AutoGradingTool database exists...");

            if (PRN232_G9_AutoGradingToolDbContext.Database.EnsureCreated())
            {
                logger.LogInformation("PRN232_G9_AutoGradingTool database was created successfully.");
            }
            else
            {
                logger.LogInformation("PRN232_G9_AutoGradingTool database already exists.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while ensuring PRN232_G9_AutoGradingTool database exists!");
            throw;
        }
    }
}

