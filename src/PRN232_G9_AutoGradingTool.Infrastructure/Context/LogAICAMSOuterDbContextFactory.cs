using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

public class PRN232_G9_AutoGradingToolOuterDbContextFactory : IDesignTimeDbContextFactory<PRN232_G9_AutoGradingToolOuterDbContext>
{
    public PRN232_G9_AutoGradingToolOuterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PRN232_G9_AutoGradingToolOuterDbContext>();

        // Ưu tiên đọc từ Environment Variables (docker env) trước
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OuterDbConnection");

        // Nếu không có trong env, fallback về appsettings.json
        if (string.IsNullOrEmpty(connectionString))
        {
            var webAppPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PRN232_G9_AutoGradingTool.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(webAppPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            connectionString = configuration.GetConnectionString("OuterDbConnection");
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'OuterDbConnection' not found in environment variables or appsettings.json.");
        }

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.MigrationsAssembly(typeof(PRN232_G9_AutoGradingToolOuterDbContext).Assembly.FullName);

                // CRITICAL: Use snake_case for migrations history table
                // This ensures compatibility with UseSnakeCaseNamingConvention()
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
            })
            // CRITICAL: Apply snake_case naming convention to all tables and columns
            .UseSnakeCaseNamingConvention();

        return new PRN232_G9_AutoGradingToolOuterDbContext(optionsBuilder.Options);
    }
}
