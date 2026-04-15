using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PRN232_G9_AutoGradingTool.Application.Common.Behaviors;
using FluentValidation;

namespace PRN232_G9_AutoGradingTool.Application;

public static class ApplicationDependencyInjection
{
    /// <summary>
    /// Add application services to the dependency container
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            
            // Add pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });
        
        // Register AutoMapper with DI support (allows constructor injection in resolvers)
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Register FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Configure Localization
        // ResourcesPath is not needed when using IStringLocalizerFactory.Create(Type)
        // .NET will automatically find resource files in the assembly containing the type
        services.AddLocalization();
        
        return services;
    }
}