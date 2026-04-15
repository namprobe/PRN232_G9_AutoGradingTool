using System.Text.RegularExpressions;
namespace PRN232_G9_AutoGradingTool.API.Configurations;

public static class EnvironmentConfiguration
{
    /// <summary>
    /// Load .env files (root and environment) into Environment and IConfiguration
    /// 
    /// Priority Order (highest to lowest):
    /// 1. Environment Variables (from OS/CI/CD/AWS) - Highest priority
    /// 2. .env.{environment}.local (local overrides)
    /// 3. .env.{environment} (environment-specific)
    /// 4. .env.local (local overrides)
    /// 5. .env.docker (Docker-specific)
    /// 6. .env (base configuration) - Lowest priority
    /// 
    /// This ensures CI/CD and AWS (Terraform) environment variables always take precedence.
    /// </summary>
    public static WebApplicationBuilder AddEnvironmentConfiguration(this WebApplicationBuilder builder, string serviceName)
    {
        var environment = builder.Environment.EnvironmentName;

        // IMPORTANT: Load .env files BEFORE ASP.NET Core's default configuration sources
        // This ensures environment variables (from CI/CD/AWS) have HIGHER priority
        // ASP.NET Core's default order: Command line > Environment Variables > appsettings.json
        // Our .env files are loaded as MemoryConfigurationSource, which comes after Environment Variables
        
        // Load in priority order (later files override earlier ones)
        // Base configuration (lowest priority)
        LoadEnvFileIfExists(builder, ".env");
        
        // Docker-specific overrides
        LoadEnvFileIfExists(builder, ".env.docker");
        
        // Local overrides (for development)
        LoadEnvFileIfExists(builder, ".env.local");
        
        // Environment-specific configuration
        LoadEnvFileIfExists(builder, $".env.{environment}");
        
        // Environment-specific local overrides (highest priority for .env files)
        LoadEnvFileIfExists(builder, $".env.{environment}.local");

        // Set service identification (can be overridden by environment variables)
        if (string.IsNullOrEmpty(builder.Configuration["Service:Name"]))
        {
            builder.Configuration["Service:Name"] = serviceName;
        }
        if (string.IsNullOrEmpty(builder.Configuration["Service:Environment"]))
        {
            builder.Configuration["Service:Environment"] = environment;
        }

        return builder;
    }

    private static void LoadEnvFileIfExists(WebApplicationBuilder builder, string fileName)
    {
        try
        {
            var repoRoot = Directory.GetCurrentDirectory();
            var possiblePaths = new[]
            {
                Path.Combine(repoRoot, fileName),
                Path.Combine(repoRoot, "src", "PRN232_G9_AutoGradingTool.API", fileName),
                Path.Combine(repoRoot, "docker", fileName)
            };

            foreach (var path in possiblePaths)
            {
                if (!File.Exists(path)) continue;
                var lines = File.ReadAllLines(path);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    // KEY=VALUE (allow export KEY=VALUE)
                    var m = Regex.Match(line, @"^(?:export\s+)?(?<key>[A-Za-z0-9_\.\-]+)=(?<val>.*)$");
                    if (!m.Success) continue;
                    var key = m.Groups["key"].Value;
                    var val = m.Groups["val"].Value;

                    // Remove surrounding quotes if present
                    if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                    {
                        val = val.Substring(1, val.Length - 2);
                    }

                    // Check if environment variable already exists (from CI/CD/AWS)
                    // Environment variables from CI/CD/AWS have HIGHEST priority
                    var existingEnvVar = Environment.GetEnvironmentVariable(key);
                    var configKey = key.Replace("__", ":");
                    var existingConfigValue = builder.Configuration[configKey];
                    
                    // Only set if not already set by environment variable
                    // This preserves CI/CD/AWS environment variables which have highest priority
                    if (string.IsNullOrEmpty(existingEnvVar))
                    {
                        // Set OS environment variable (for compatibility)
                        Environment.SetEnvironmentVariable(key, val);
                    }
                    
                    // Populate IConfiguration with hierarchical keys using __ => :
                    // Only if not already set (preserve existing values from environment variables)
                    // ASP.NET Core will load environment variables AFTER this, so they will override
                    // But we check here to avoid unnecessary overwrites
                    if (string.IsNullOrEmpty(existingConfigValue) && string.IsNullOrEmpty(existingEnvVar))
                    {
                        builder.Configuration[configKey] = val;
                    }
                }
            }
        }
        catch
        {
            // Fail safe: ignore errors while loading env files
        }
    }
}
