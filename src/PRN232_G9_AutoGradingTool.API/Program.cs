using Microsoft.AspNetCore.Http.Features;
using PRN232_G9_AutoGradingTool.API.Configurations;
using PRN232_G9_AutoGradingTool.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Multipart (2× zip lớn): Kestrel mặc định ~28MB; FormOptions cần đủ cho tổng request
const long maxUploadBytes = 200L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUploadBytes);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUploadBytes);

// Load environment variables from .env files into IConfiguration
builder.AddEnvironmentConfiguration("PRN232_G9_AutoGradingTool");

// Configure all services
builder.ConfigureServices();

var app = builder.Build()
    .ConfigurePipeline();

// Configure application with proper order: migrations → hangfire → jobs
await app.ConfigureApplicationAsync();

app.Run();
