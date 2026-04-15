﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Services;
using System.Diagnostics;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Factories;

/// <summary>
/// Factory for creating appropriate file service providers
/// </summary>
public class FileServiceFactory : IFileServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FileStorageSettings _storageSettings;
    private readonly ILogger<FileServiceFactory> _logger;

    public FileServiceFactory(
        IServiceProvider serviceProvider,
        IOptions<FileStorageSettings> storageSettings,
        ILogger<FileServiceFactory> logger
    )
    {
        _serviceProvider = serviceProvider;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Create the appropriate file service based on configuration
    /// </summary>
    /// <returns>File service instance</returns>
    public IFileService CreateFileService()
    {
        var traceId = Activity.Current?.TraceId.ToString();

        _logger.LogDebug(
            "Creating file service. TraceId: {TraceId}, ProviderType: {ProviderType}, MaxFileSizeBytes: {MaxFileSizeBytes}, AllowedExtensionsCount: {AllowedExtensionsCount}, LocalRootPath: {LocalRootPath}, HasBaseUrl: {HasBaseUrl}",
            traceId,
            _storageSettings.ProviderType,
            _storageSettings.MaxFileSizeBytes,
            _storageSettings.AllowedExtensions?.Length ?? 0,
            _storageSettings.Local?.RootPath,
            !string.IsNullOrWhiteSpace(_storageSettings.BaseUrl));
        
        return _storageSettings.ProviderType switch
        {
            StorageProviderType.LocalStorage => CreateLocalFileService(traceId),
            
            // Uncomment when implemented and registered in DI
            // StorageProviderType.AmazonS3 => _serviceProvider.GetRequiredService<S3FileService>(),
            StorageProviderType.AmazonS3 => throw CreateProviderNotImplementedException(traceId, _storageSettings.ProviderType),
            
            _ => throw new ArgumentException($"Unsupported storage provider type: {_storageSettings.ProviderType}")
        };
    }

    private IFileService CreateLocalFileService(string? traceId)
    {
        _logger.LogInformation(
            "Selected file service provider. TraceId: {TraceId}, ProviderType: {ProviderType}, Reason: {Reason}",
            traceId,
            StorageProviderType.LocalStorage,
            "FileStorage:ProviderType configured to LocalStorage");

        return _serviceProvider.GetRequiredService<LocalFileService>();
    }

    private Exception CreateProviderNotImplementedException(string? traceId, StorageProviderType providerType)
    {
        _logger.LogWarning(
            "File service provider not implemented. TraceId: {TraceId}, ProviderType: {ProviderType}",
            traceId,
            providerType);

        return new NotImplementedException("Amazon S3 storage is not yet implemented. Please implement S3FileService.");
    }
}
