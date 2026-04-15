using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

public class PRN232_G9_AutoGradingToolDbContextFactory : IDesignTimeDbContextFactory<PRN232_G9_AutoGradingToolDbContext>
{
    public PRN232_G9_AutoGradingToolDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PRN232_G9_AutoGradingToolDbContext>();

        // Ưu tiên đọc từ Environment Variables (docker env) trước
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        // Nếu không có trong env, fallback về appsettings.json
        if (string.IsNullOrEmpty(connectionString))
        {
            var webAppPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PRN232_G9_AutoGradingTool.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(webAppPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in environment variables or appsettings.json.");
        }

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.MigrationsAssembly(typeof(PRN232_G9_AutoGradingToolDbContext).Assembly.FullName);

                // CRITICAL: Use snake_case for migrations history table
                // This ensures compatibility with UseSnakeCaseNamingConvention()
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
            })
            // CRITICAL: Apply snake_case naming convention to all tables and columns
            // This maps PascalCase C# properties to snake_case PostgreSQL columns
            // Example: AppUser.FirstName -> app_users.first_name
            .UseSnakeCaseNamingConvention();

        return new PRN232_G9_AutoGradingToolDbContext(optionsBuilder.Options);
    }
}
