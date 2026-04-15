using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;
using PRN232_G9_AutoGradingTool.Infrastructure.Repositories;
using PRN232_G9_AutoGradingTool.Infrastructure.Services;
using PRN232_G9_AutoGradingTool.Infrastructure.Configurations;
using PRN232_G9_AutoGradingTool.Infrastructure.Factories;
using System.Threading;
using System.Threading.Tasks;

namespace PRN232_G9_AutoGradingTool.Infrastructure;

public static class InfrastructureDependencyInjection
{

    /// <summary>
    /// Add infrastructure services to the dependency container
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string not found. Please se configure it in appsettings.json");
        }

        // Configure database contexts
        services.AddDbContextPool<PRN232_G9_AutoGradingToolDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PRN232_G9_AutoGradingToolDbContext).Assembly.FullName);
                npgsql.CommandTimeout(30);
            });
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
        });

        // Configure Outer Database Context for external services and Hangfire
        var outerConnectionString = configuration.GetConnectionString("OuterDbConnection");
        if (!string.IsNullOrEmpty(outerConnectionString))
        {
            services.AddDbContextPool<PRN232_G9_AutoGradingToolOuterDbContext>(options =>
            {
                options.UseNpgsql(outerConnectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(PRN232_G9_AutoGradingToolOuterDbContext).Assembly.FullName);
                    npgsql.CommandTimeout(30);
                });
            });
        }
        // Map DbContext for services that depend on base DbContext (e.g., UnitOfWork)
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>());

        // Configure Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;  // Changed to true for email verification

            // SignIn settings  
            options.SignIn.RequireConfirmedEmail = true;   // Changed to true for email verification
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<PRN232_G9_AutoGradingToolDbContext>()
        .AddDefaultTokenProviders();

        // Configure email token lifespan (24 hours)
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });

        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));


        // Configure Hangfire
        // Service Account hoàn toàn tự động - không cần token management jobs
        var useHangfire = configuration.GetValue("Hangfire:UseOuterDatabase", false);
        if (useHangfire && !string.IsNullOrEmpty(outerConnectionString))
        {
            services.AddHangfireServices(configuration);
        }

        // Configure Email settings
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Configure File Storage settings
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        // Register repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationFactory, NotificationFactory>();
        services.AddScoped<IFirebaseService, FirebaseService>();
        
        // Register localization service
        services.AddScoped<ILocalizationService, LocalizationService>();
        // File storage services
        services.AddScoped<LocalFileService>(); // Local storage implementation
        // services.AddScoped<S3FileService>(); // Uncomment when S3 is implemented
        services.AddScoped<IFileServiceFactory, FileServiceFactory>(); // Register factory as scoped to resolve scoped services

        services.AddHostedService<InfrastructureStartupLoggingHostedService>();
        
        return services;
    }
}

internal sealed class InfrastructureStartupLoggingHostedService : IHostedService
{
    private readonly ILogger<InfrastructureStartupLoggingHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<FileStorageSettings> _fileStorageOptions;

    public InfrastructureStartupLoggingHostedService(
        ILogger<InfrastructureStartupLoggingHostedService> logger,
        IConfiguration configuration,
        IOptions<FileStorageSettings> fileStorageOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _fileStorageOptions = fileStorageOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var hasDefaultConnectionString = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection"));
        var hasOuterConnectionString = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("OuterDbConnection"));
        var useHangfire = _configuration.GetValue("Hangfire:UseOuterDatabase", false);
        var fs = _fileStorageOptions.Value;

        _logger.LogInformation(
            "Infrastructure services configured. HasDefaultConnectionString: {HasDefaultConnectionString}, HasOuterConnectionString: {HasOuterConnectionString}, HangfireUseOuterDatabase: {HangfireUseOuterDatabase}, FileStorageProviderType: {FileStorageProviderType}, MaxFileSizeBytes: {MaxFileSizeBytes}, AllowedExtensionsCount: {AllowedExtensionsCount}",
            hasDefaultConnectionString,
            hasOuterConnectionString,
            useHangfire,
            fs.ProviderType,
            fs.MaxFileSizeBytes,
            fs.AllowedExtensions?.Length ?? 0);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
