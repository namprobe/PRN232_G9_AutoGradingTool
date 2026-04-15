using PRN232_G9_AutoGradingTool.API.Configurations;
using PRN232_G9_AutoGradingTool.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env files into IConfiguration
builder.AddEnvironmentConfiguration("PRN232_G9_AutoGradingTool");

// Configure all services
builder.ConfigureServices();

var app = builder.Build()
    .ConfigurePipeline();

// Configure application with proper order: migrations → hangfire → jobs
await app.ConfigureApplicationAsync();

app.Run();
