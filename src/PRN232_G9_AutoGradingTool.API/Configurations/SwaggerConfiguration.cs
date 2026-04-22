using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PRN232_G9_AutoGradingTool.API.Configurations;

/// <summary>
/// Configuration for Swagger UI
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configure Swagger generation options
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Configure basic information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PRN232 G9 — Auto Grading API",
                Version = "v1",
                Description =
                    "Nhóm endpoint trên Swagger UI được sắp theo tiền tố **01–09** (xem từng tag). " +
                    "Luồng gợi ý: **01** lấy JWT → **02–08** quản trị đề & bài nộp → **09** nộp bài SV."
            });

            options.TagActionsBy(api =>
            {
                var path = api.RelativePath;
                return new[] { SwaggerTagResolver.Resolve(path) };
            });

            options.OrderActionsBy(apiDesc =>
            {
                var path = apiDesc.RelativePath ?? "";
                var tag = SwaggerTagResolver.Resolve(path);
                return $"{tag}\u2003{apiDesc.HttpMethod}\u2003{path}";
            });

            options.DocumentFilter<SwaggerTagDescriptionDocumentFilter>();
            
            // Configure JWT authentication in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
            
            // Customize operation IDs to include controller name
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                    return $"{controllerName}_{methodInfo.Name}";
                }
                return null;
            });
        });
        
        return services;
    }
    
    /// <summary>
    /// Configure Swagger middleware
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger(c =>
        {
            // Using the default route template to match the SwaggerUI expectation
            // c.RouteTemplate = "api-docs/{documentName}/swagger.json";
        });
        
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PRN232 G9 Auto Grading — v1");
            options.RoutePrefix = "swagger";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayOperationId();
            options.EnableFilter();
            
            // Enable tag grouping
            options.EnableTryItOutByDefault();
            
            // Custom CSS to style main tags differently
            options.InjectStylesheet("/swagger-custom/custom-swagger-ui.css");
        });
        
        return app;
    }
}