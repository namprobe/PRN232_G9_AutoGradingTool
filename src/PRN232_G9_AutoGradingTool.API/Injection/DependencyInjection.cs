using PRN232_G9_AutoGradingTool.API.Attributes;
using PRN232_G9_AutoGradingTool.API.Middlewares;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PRN232_G9_AutoGradingTool.API.Injection;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HttpContextAccessor
        services.AddHttpContextAccessor();
        
        // Register FileService with factory pattern (based on appsettings.json configuration)
        // The factory automatically selects the appropriate provider based on FileStorage:ProviderType
        services.AddScoped<IFileService>(provider =>
        {
            var factory = provider.GetRequiredService<IFileServiceFactory>();
            return factory.CreateFileService();
        });
        
        // Register role-based access filters
        services.AddScoped<StaffRoleAccessFilter>();
        services.AddScoped<AdminRoleAccessFilter>();

        // Register validation configuration
        services.AddValidationConfiguration();

        services.AddHostedService<ApiStartupLoggingHostedService>();


        return services;
    }

    public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app)
    {
        var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("PRN232_G9_AutoGradingTool.API.Startup");

        logger?.LogInformation(
            "Applying API middleware pipeline. GlobalExceptionHandling: {GlobalExceptionHandling}, JwtMiddleware: {JwtMiddleware}",
            true,
            true);

        // Use global exception handling
        app.UseGlobalExceptionHandling();

        // Use JWT middleware
        app.UseJwtMiddleware();

        logger?.LogInformation("API middleware pipeline applied");

        return app;
    }
}

internal sealed class ApiStartupLoggingHostedService : IHostedService
{
    private readonly ILogger<ApiStartupLoggingHostedService> _logger;

    public ApiStartupLoggingHostedService(ILogger<ApiStartupLoggingHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "API services configured. HttpContextAccessor: {HttpContextAccessor}, ValidationFilter: {ValidationFilter}, RoleFilters: {RoleFilters}, FileServiceFactory: {FileServiceFactory}",
            true,
            true,
            true,
            true);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
