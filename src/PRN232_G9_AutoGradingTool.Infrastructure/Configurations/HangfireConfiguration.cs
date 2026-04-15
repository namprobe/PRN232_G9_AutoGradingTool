using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Infrastructure.Services;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Configurations;

public static class HangfireConfiguration
{
    /// <summary>
    /// Add Hangfire services to the dependency container
    /// Uses the same OuterDbConnection as token management for consistency
    /// </summary>
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure logging to suppress Hangfire initialization errors
        // These errors occur during service registration before migrations complete
        // Hangfire will work correctly after migrations and schema creation
        services.AddLogging(builder =>
        {
            // Suppress Hangfire initialization errors - they will be resolved after migrations
            builder.AddFilter("Hangfire", LogLevel.Warning);
            builder.AddFilter("Hangfire.Processing", LogLevel.Warning);
        });

        // Get connection string - use OuterDbConnection for consistency with token management
        var useOuterDatabase = configuration.GetValue("Hangfire:UseOuterDatabase", true);
        var connectionString = useOuterDatabase 
            ? configuration.GetConnectionString("OuterDbConnection")
            : configuration["Hangfire:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                useOuterDatabase 
                    ? "OuterDbConnection string is required for Hangfire when UseOuterDatabase is true"
                    : "Hangfire database connection string is required");
        }
        
        // NOTE: Hangfire Storage initialization will happen when first used
        // Schema creation is delayed until ConfigureHangfireStorageAsync is called
        // This ensures migrations are completed before Hangfire tries to access the database

        // Get Hangfire settings from configuration
        var commandBatchMaxTimeout = configuration.GetValue("Hangfire:CommandBatchMaxTimeout", 300);
        var slidingInvisibilityTimeout = configuration.GetValue("Hangfire:SlidingInvisibilityTimeout", 300);
        var queuePollInterval = configuration.GetValue("Hangfire:QueuePollInterval", 0);
        var useRecommendedIsolationLevel = configuration.GetValue("Hangfire:UseRecommendedIsolationLevel", true);
        var disableGlobalLocks = configuration.GetValue("Hangfire:DisableGlobalLocks", true);

        // Get retry settings
        var retryAttempts = configuration.GetValue("Hangfire:Retry:Attempts", 3);
        var retryDelayFirst = configuration.GetValue("Hangfire:Retry:DelayInSeconds:First", 60);
        var retryDelaySecond = configuration.GetValue("Hangfire:Retry:DelayInSeconds:Second", 300);
        var retryDelayThird = configuration.GetValue("Hangfire:Retry:DelayInSeconds:Third", 600);

        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
            {
                // Let Hangfire prepare schema if necessary on first use
                PrepareSchemaIfNecessary = true
            })
            .UseFilter(new AutomaticRetryAttribute 
            { 
                Attempts = retryAttempts,
                DelaysInSeconds = new[] { retryDelayFirst, retryDelaySecond, retryDelayThird }
                    .Take(retryAttempts)
                    .ToArray()
            }));

        // Get server settings from configuration
        var heartbeatInterval = configuration.GetValue("Hangfire:HeartbeatInterval", 30);
        var workerCount = configuration.GetValue("Hangfire:WorkerCount", 0);
        
        // Get queues from configuration - support both array and comma-separated string
        string[] queues;
        var queuesSection = configuration.GetSection("Hangfire:Queues");
        if (queuesSection.Exists() && queuesSection.Get<string[]>() != null)
        {
            // Array format: Hangfire:Queues:0=queue1, Hangfire:Queues:1=queue2
            queues = queuesSection.Get<string[]>() ?? new[] { "default" };
        }
        else
        {
            // String format: Hangfire:Queues=queue1,queue2,queue3
            var queuesString = configuration.GetValue<string>("Hangfire:Queues");
            if (!string.IsNullOrWhiteSpace(queuesString))
            {
                queues = queuesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(q => q.Trim())
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .ToArray();
            }
            else
            {
                // Fallback to default queues
                queues = new[] { "default" };
            }
        }

        // Add Hangfire server with ordered queues
        // Server will start automatically, but it will wait for schema to be created
        // Schema creation happens in ConfigureHangfireStorageAsync before server tries to use it
        services.AddHangfireServer(options =>
        {
            options.HeartbeatInterval = TimeSpan.FromSeconds(heartbeatInterval);
            options.Queues = queues;
            if (workerCount > 0)
            {
                options.WorkerCount = workerCount;
            }
        });

        return services;
    }

    /// <summary>
    /// Cấu hình Hangfire storage sử dụng Outer Database đã có
    /// Đảm bảo database sẵn sàng và migrations đã hoàn tất trước khi cho phép Hangfire khởi tạo
    /// </summary>
    public static async Task ConfigureHangfireStorageAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("HangfireConfiguration");

            try
            {
                var useOuterDatabase = configuration.GetValue("Hangfire:UseOuterDatabase", true);
                var hangfireConn = useOuterDatabase
                    ? configuration.GetConnectionString("OuterDbConnection")
                    : configuration["Hangfire:ConnectionString"];

                if (string.IsNullOrEmpty(hangfireConn))
                {
                    logger.LogError("No connection string found for Hangfire storage");
                    throw new InvalidOperationException("Hangfire connection string is required");
                }

                // Verify DB connectivity
                logger.LogInformation("Verifying Hangfire database connectivity...");
                await WaitForDatabaseReadyAsync(hangfireConn, logger);

                logger.LogInformation("Hangfire storage verified successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring Hangfire storage");
                throw;
            }
    }
    
    /// <summary>
    /// Đợi database sẵn sàng với retry logic
    /// </summary>
    private static async Task WaitForDatabaseReadyAsync(string connectionString, ILogger logger, int maxRetries = 5, int delaySeconds = 2)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Test query to ensure DB ready
                await using var cmd = new NpgsqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();

                logger.LogInformation("Database is ready and accessible (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Database not ready yet (attempt {Attempt}/{MaxRetries}). Retrying in {DelaySeconds}s...", attempt, maxRetries, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to database after {MaxRetries} attempts", maxRetries);
                throw new InvalidOperationException($"Database is not ready. Please ensure migrations are completed.", ex);
            }
        }
    }
    

    /// <summary>
    /// Kiểm tra database Hangfire có tồn tại không
    /// </summary>
    private static Task<bool> CheckHangfireDatabaseExistsAsync(string connectionString, string databaseName, ILogger logger)
    {
        // For PostgreSQL we assume DB existence is verified by connection attempt in WaitForDatabaseReadyAsync
        return Task.FromResult(true);
    }

    /// <summary>
    /// Trích xuất tên database từ connection string
    /// </summary>
    private static string ExtractDatabaseName(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return builder.Database;
    }
    
    /// <summary>
    /// Tạo Hangfire schema thủ công sau khi migrations đã hoàn tất
    /// </summary>
    private static Task CreateHangfireSchemaAsync(string connectionString, ILogger logger)
    {
        // We rely on Hangfire.PostgreSql's PrepareSchemaIfNecessary option to create schema on first use.
        logger.LogInformation("Schema creation is delegated to Hangfire.PostgreSql (PrepareSchemaIfNecessary=true)");
        return Task.CompletedTask;
    }


    /// <summary>
    /// Configure Hangfire dashboard and initialize recurring jobs
    /// Token management jobs are disabled since we're using Service Account authentication
    /// </summary>
    public static void UseHangfireConfiguration(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("HangfireConfiguration");
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // Only log detailed configuration in development
        if (environment.IsDevelopment())
        {
            logger.LogInformation("=== HANGFIRE CONFIGURATION ===");
            logger.LogInformation("🔧 DEVELOPMENT MODE: Dashboard accessible without authentication");
            logger.LogInformation("🗄️ Database: Shared OuterDb");
            logger.LogInformation("🌐 Dashboard: /hangfire (no auth required)");
            logger.LogInformation("� Email: Using Service Account (no token management needed)");
        }

        // All OAuth token management jobs are disabled
        // Using Service Account authentication instead
        
        logger.LogInformation("Hangfire configured successfully (Service Account mode - no token jobs needed)");
    }
}
