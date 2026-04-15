using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace PRN232_G9_AutoGradingTool.Application.Common.Validators;

/// <summary>
/// Simplified file validation for all entities - supports Image, Video, Document, All file types
/// Flexible max size with switch case handling
/// </summary>
public static class FileValidatorExtension
{
    #region Core File Validation
    
    /// <summary>
    /// Main file validation method with flexible file type and size options
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFile?> ValidFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, 
        FileType fileType, double maxSizeInMB = 5.0)
    {
        return ruleBuilder
            .Must(file => BeValidExtension(file, fileType))
            .WithMessage(file => GetExtensionErrorMessage(fileType))
            .Must(file => BeValidFileSize(file, maxSizeInMB))
            .WithMessage($"File size must not exceed {maxSizeInMB}MB")
            .Must(BeValidFileName)
            .WithMessage("File name contains invalid characters")
            .When(x => x != null);
    }
    
    /// <summary>
    /// Validates file collection with flexible type and size
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFileCollection?> ValidFileCollection<T>(this IRuleBuilder<T, IFormFileCollection?> ruleBuilder,
        FileType fileType, double totalMaxSizeInMB = 50.0, int maxFileCount = 10)
    {
        return ruleBuilder
            .Must(files => BeValidFileCount(files, maxFileCount))
            .WithMessage($"Cannot upload more than {maxFileCount} files")
            .Must(files => BeValidCollectionSize(files, totalMaxSizeInMB))
            .WithMessage($"Total file size must not exceed {totalMaxSizeInMB}MB")
            .Must(files => BeValidCollectionExtensions(files, fileType))
            .WithMessage(files => GetExtensionErrorMessage(fileType))
            .When(x => x != null);
    }
    
    #endregion
    
    #region Enum and Extensions
    
    public enum FileType
    {
        Image,      // jpg, jpeg, png, gif, webp, bmp, svg
        Video,      // mp4, avi, mov, wmv, flv, webm
        Document,   // pdf, doc, docx, txt, xlsx, xls, ppt, pptx, csv
        All         // Any file type
    }
    
    private static string[] GetAllowedExtensions(FileType fileType)
    {
        return fileType switch
        {
            FileType.Image => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" },
            FileType.Video => new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" },
            FileType.Document => new[] { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".xls", ".ppt", ".pptx", ".csv" },
            FileType.All => new[] { "*" }, // Special case - any extension allowed
            _ => Array.Empty<string>()
        };
    }
    
    private static string GetExtensionErrorMessage(FileType fileType)
    {
        return fileType switch
        {
            FileType.Image => "File must be an image (jpg, jpeg, png, gif, webp, bmp, svg)",
            FileType.Video => "File must be a video (mp4, avi, mov, wmv, flv, webm, mkv)",
            FileType.Document => "File must be a document (pdf, doc, docx, txt, xlsx, xls, ppt, pptx, csv)",
            FileType.All => "Invalid file format",
            _ => "Invalid file type"
        };
    }
    
    #endregion
    
    #region Private Helper Methods
    
    private static bool BeValidExtension(IFormFile? file, FileType fileType)
    {
        if (file == null) return true;
        
        if (fileType == FileType.All) return true; // Allow any extension
        
        var allowedExtensions = GetAllowedExtensions(fileType);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Any(ext => ext.ToLowerInvariant() == extension);
    }
    
    private static bool BeValidFileSize(IFormFile? file, double maxSizeInMB)
    {
        if (file == null) return true;
        
        var maxSizeInBytes = (long)(maxSizeInMB * 1024 * 1024);
        return file.Length <= maxSizeInBytes;
    }
    
    private static bool BeValidFileName(IFormFile? file)
    {
        if (file == null) return true;
        
        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        
        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(invalidChars.Contains);
    }
    
    private static bool BeValidFileCount(IFormFileCollection? files, int maxCount)
    {
        if (files == null) return true;
        return files.Count <= maxCount;
    }
    
    private static bool BeValidCollectionSize(IFormFileCollection? files, double maxTotalSizeInMB)
    {
        if (files == null) return true;
        
        var maxSizeInBytes = (long)(maxTotalSizeInMB * 1024 * 1024);
        var totalSize = files.Sum(f => f.Length);
        return totalSize <= maxSizeInBytes;
    }
    
    private static bool BeValidCollectionExtensions(IFormFileCollection? files, FileType fileType)
    {
        if (files == null) return true;
        return files.All(file => BeValidExtension(file, fileType));
    }
    
    #endregion

    #region Convenience Methods

    /// <summary>
    /// Validates image files with default 5MB size limit
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFile?> ValidImageFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, double maxSizeInMB = 5.0)
    {
        return ruleBuilder.ValidFile(FileType.Image, maxSizeInMB);
    }

    /// <summary>
    /// Validates video files with default 100MB size limit
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFile?> ValidVideoFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, double maxSizeInMB = 100.0)
    {
        return ruleBuilder.ValidFile(FileType.Video, maxSizeInMB);
    }

    /// <summary>
    /// Validates document files with default 10MB size limit
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFile?> ValidDocumentFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, double maxSizeInMB = 10.0)
    {
        return ruleBuilder.ValidFile(FileType.Document, maxSizeInMB);
    }

    /// <summary>
    /// Validates any file type with default 20MB size limit
    /// </summary>
    public static IRuleBuilderOptions<T, IFormFile?> ValidAnyFile<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, double maxSizeInMB = 20.0)
    {
        return ruleBuilder.ValidFile(FileType.All, maxSizeInMB);
    }

    #endregion
}
