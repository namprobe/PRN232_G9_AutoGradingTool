
namespace PRN232_G9_AutoGradingTool.API.Configurations;

public static class LoggingConfiguration
{
    // Categories to be completely filtered out from log files
    private static readonly string[] ExcludedCategories = new[] 
    {
        "Microsoft.AspNetCore.StaticFiles",
        "Microsoft.AspNetCore.Hosting.Diagnostics"
    };
    
    public static WebApplicationBuilder AddLoggingConfiguration(this WebApplicationBuilder builder)
    {
        // Configure logging to filter out unnecessary logs
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        var isDevelopment = builder.Environment.IsDevelopment();
        
        // Create a dictionary with category-specific log levels
        var filterDictionary = new Dictionary<string, LogLevel>
        {
            // Completely suppress all database command logs
            ["Microsoft.EntityFrameworkCore.Database.Command"] = LogLevel.None,
            // Set higher threshold for other EF messages
            ["Microsoft.EntityFrameworkCore"] = LogLevel.Warning,
            ["Microsoft.Data.SqlClient"] = LogLevel.Warning
        };
        
        // Add completely excluded categories
        foreach (var category in ExcludedCategories)
        {
            filterDictionary[category] = LogLevel.None;
        }

        // Add file logging with level based on environment
        // Development: Log Information and above
        // Production: Log Warning and above (hide Information logs)
        var fileLogLevel = isDevelopment ? LogLevel.Information : LogLevel.Warning;
        
        // Add file logging with custom filter dictionary
        builder.Logging.AddFile(
            "Logs/PRN232_G9_AutoGradingTool-{Date}.txt",
            LogLevel.Information,
            filterDictionary,
            fileSizeLimitBytes: 10 * 1024 * 1024,  // 10 MB size limit
            retainedFileCountLimit: 30);  // Keep logs for 30 days
            
        // Add additional event filter
        builder.Logging.AddFilter((category, level) => 
        {
            // If it's an EF Core DB Command log, filter it out completely
            if (category == "Microsoft.EntityFrameworkCore.Database.Command")
            {
                return false;
            }
            
            // For all other categories, use standard filtering
            return level >= LogLevel.Information;
        });
        
        return builder;
    }
    
    public static ILogger CreateStartupLogger()
    {
        // Create a dictionary with category-specific log levels
        var filterDictionary = new Dictionary<string, LogLevel>
        {
            // Completely suppress all database command logs
            ["Microsoft.EntityFrameworkCore.Database.Command"] = LogLevel.None,
            // Set higher threshold for other EF messages
            ["Microsoft.EntityFrameworkCore"] = LogLevel.Warning,
            ["Microsoft.Data.SqlClient"] = LogLevel.Warning
        };
        
        // Add completely excluded categories
        foreach (var category in ExcludedCategories)
        {
            filterDictionary[category] = LogLevel.None;
        }
        
        return LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            
            // Use the filter dictionary with the file provider
            builder.AddFile(
                "Logs/PRN232_G9_AutoGradingTool-{Date}.txt",
                LogLevel.Information,
                filterDictionary,
                fileSizeLimitBytes: 10 * 1024 * 1024,  // 10 MB size limit
                retainedFileCountLimit: 30);  // Keep logs for 30 days
                
            // Add additional event filter
            builder.AddFilter((category, level) => 
            {
                // If it's an EF Core DB Command log, filter it out completely
                if (category == "Microsoft.EntityFrameworkCore.Database.Command")
                {
                    return false;
                }
                
                // For all other categories, use standard filtering
                return level >= LogLevel.Information;
            });
        }).CreateLogger("Program");
    }
} 