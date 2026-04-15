﻿﻿﻿﻿﻿﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using System.Diagnostics;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

/// <summary>
/// Implementation for local file system storage
/// </summary>

public class LocalFileService : IFileService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<LocalFileService> _logger;
    private readonly FileStorageSettings _storageSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalFileService(
        IWebHostEnvironment webHostEnvironment,
        ILogger<LocalFileService> logger,
        IOptions<FileStorageSettings> storageSettings,
        IHttpContextAccessor httpContextAccessor)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _storageSettings = storageSettings.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Bulk upload files to storage
    /// </summary>
    public async Task<List<string?>> UploadFilesBulkAsync(List<IFormFile> files, List<string> fileNames, string subDirectory = "", CancellationToken cancellationToken = default)
    {
        var results = new List<string?>();
        for (int i = 0; i < files.Count; i++)
        {
            try
            {
                var file = files[i];
                var fileName = fileNames[i];
                var path = await UploadFileAsync(file, fileName, subDirectory, cancellationToken);
                results.Add(path);
            }
            catch
            {
                results.Add(null);
            }
        }
        return results;
    }

    /// <summary>
    /// Bulk delete files from storage
    /// </summary>
    public async Task<List<bool>> DeleteFilesBulkAsync(List<string> filePaths, CancellationToken cancellationToken = default)
    {
        var results = new List<bool>();
        foreach (var filePath in filePaths)
        {
            try
            {
                var deleted = await DeleteFileAsync(filePath, cancellationToken);
                results.Add(deleted);
            }
            catch
            {
                results.Add(false);
            }
        }
        return results;
    }

    /// <summary>
    /// Upload file from IFormFile
    /// </summary>
    public async Task<string> UploadFileAsync(
        IFormFile file,
        string fileName,
        string subDirectory = "",
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty", nameof(file));
        }

        if (file.Length > _storageSettings.MaxFileSizeBytes)
        {
            throw new ArgumentException(
                $"File size exceeds the maximum allowed size ({_storageSettings.MaxFileSizeBytes / 1024 / 1024}MB)");
        }

        try
        {
            // Validate file extension if restrictions are set
            if (_storageSettings.AllowedExtensions?.Length > 0)
            {
                var extension = Path.GetExtension(fileName);
                if (!_storageSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"File extension {extension} is not allowed");
                }
            }

            // Determine uploads directory path
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, _storageSettings.Local?.RootPath ?? "uploads");

            // Add subdirectory if specified
            if (!string.IsNullOrEmpty(subDirectory))
            {
                uploadsFolder = Path.Combine(uploadsFolder, subDirectory);
            }

            // Create directory if it doesn't exist
            EnsureDirectoryExists(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            // Create relative path for storage in DB
            var relativePath = string.IsNullOrEmpty(subDirectory)
                ? Path.Combine(_storageSettings.Local?.RootPath ?? "uploads", fileName)
                : Path.Combine(_storageSettings.Local?.RootPath ?? "uploads", subDirectory, fileName);

            relativePath = relativePath.Replace('\\', '/');
            if (relativePath.StartsWith('/'))
            {
                relativePath = relativePath.Substring(1);
            }

            _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Upload file from stream
    /// </summary>
    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string subDirectory = "",
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty", nameof(fileStream));
        }

        if (fileStream.Length > _storageSettings.MaxFileSizeBytes)
        {
            throw new ArgumentException(
                $"File size exceeds the maximum allowed size ({_storageSettings.MaxFileSizeBytes / 1024 / 1024}MB)");
        }

        try
        {
            // Validate file extension if restrictions are set
            if (_storageSettings.AllowedExtensions?.Length > 0)
            {
                var extension = Path.GetExtension(fileName);
                if (!_storageSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"File extension {extension} is not allowed");
                }
            }

            // Determine uploads directory path
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, _storageSettings.Local?.RootPath ?? "uploads");

            // Add subdirectory if specified
            if (!string.IsNullOrEmpty(subDirectory))
            {
                uploadsFolder = Path.Combine(uploadsFolder, subDirectory);
            }

            // Create directory if it doesn't exist
            EnsureDirectoryExists(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save the file
            using (var outputStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(outputStream, cancellationToken);
            }

            // Create relative path for storage in DB
            var relativePath = string.IsNullOrEmpty(subDirectory)
                ? Path.Combine(uploadsFolder, fileName)
                : Path.Combine(uploadsFolder, subDirectory, fileName);

            relativePath = relativePath.Replace('\\', '/');
            if (relativePath.StartsWith('/'))
            {
                relativePath = relativePath.Substring(1);
            }

            _logger.LogInformation("File uploaded from stream successfully: {FilePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file from stream: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Delete file from local storage
    /// </summary>
    public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Task.FromResult(false);
            }

            // Remove "uploads/" from the beginning if present
            if (filePath.StartsWith($"{_storageSettings.Local?.RootPath ?? "uploads"}/"))
            {
                filePath = filePath.Substring($"{_storageSettings.Local?.RootPath ?? "uploads"}/".Length);
            }

            // Get the complete file path
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, _storageSettings.Local?.RootPath ?? "uploads", filePath);

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", fullPath);
                return Task.FromResult(false);
            }

            // Delete the file
            File.Delete(fullPath);
            _logger.LogInformation("File deleted successfully: {FilePath}", fullPath);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Get the public URL for a file
    /// Automatically detects the base URL from HttpContext if BaseUrl is not configured
    /// This works with IIS, ngrok, and any domain deployment
    /// </summary>
    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        // Ensure relative path format
        var relativePath = filePath.Replace('\\', '/');
        if (!relativePath.StartsWith('/'))
        {
            relativePath = $"/{relativePath}";
        }

        // Priority 1: Use configured BaseUrl if available
        if (!string.IsNullOrWhiteSpace(_storageSettings.BaseUrl))
        {
            return $"{_storageSettings.BaseUrl.TrimEnd('/')}{relativePath}";
        }

        // Priority 2: Auto-detect from HttpContext (works with IIS, ngrok, any domain)
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host;
            var pathBase = request.PathBase;

            // Build base URL from current request
            var baseUrl = $"{scheme}://{host}{pathBase}";
            return $"{baseUrl.TrimEnd('/')}{relativePath}";
        }

        // Priority 3: Fallback to relative path (for background jobs, etc.)
        return relativePath;
    }

    /// <summary>
    /// Get file content from storage
    /// </summary>
    public async Task<(byte[] FileContent, string ContentType)> GetFileContentAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path is empty", nameof(filePath));
            }

            // Remove leading slash and "uploads/" prefix if present
            var cleanPath = filePath.TrimStart('/');
            if (cleanPath.StartsWith($"{_storageSettings.Local?.RootPath ?? "uploads"}/"))
            {
                cleanPath = cleanPath.Substring($"{_storageSettings.Local?.RootPath ?? "uploads"}/".Length);
            }

            // Get the complete file path
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, _storageSettings.Local?.RootPath ?? "uploads", cleanPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            // Read file content
            var fileContent = await File.ReadAllBytesAsync(fullPath, cancellationToken);

            // Determine content type based on extension
            var contentType = GetContentType(Path.GetExtension(fullPath));

            return (fileContent, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file content: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Ensure directory exists, create if not
    /// </summary>
    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogInformation("Created directory: {Path}", path);
        }
    }

    /// <summary>
    /// Get content type based on file extension
    /// </summary>
    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

