# File Validation Usage Guide - Simplified

## Overview
Thu gọn và đơn giản hóa file validation cho toàn bộ dự án. Hỗ trợ 4 loại file chính: **Image, Video, Document, All** với flexible max size.

## Core API - FileType Enum

### FileType Support
```csharp
public enum FileType
{
    Image,      // jpg, jpeg, png, gif, webp, bmp, svg
    Video,      // mp4, avi, mov, wmv, flv, webm, mkv  
    Document,   // pdf, doc, docx, txt, xlsx, xls, ppt, pptx, csv
    All         // Any file type
}
```

### Main Validation Method
```csharp
// Core method - flexible file type and size
RuleFor(x => x.File).ValidFile(FileType.Image, maxSizeInMB: 5.0);
RuleFor(x => x.File).ValidFile(FileType.Video, maxSizeInMB: 100.0);
RuleFor(x => x.File).ValidFile(FileType.Document, maxSizeInMB: 10.0);
RuleFor(x => x.File).ValidFile(FileType.All, maxSizeInMB: 20.0);
```

## Convenience Methods

### Quick Validators with Default Sizes
```csharp
// Image files (default: 5MB)
RuleFor(x => x.ImageFile).ValidImageFile();
RuleFor(x => x.ImageFile).ValidImageFile(maxSizeInMB: 3.0);

// Video files (default: 100MB)  
RuleFor(x => x.VideoFile).ValidVideoFile();
RuleFor(x => x.VideoFile).ValidVideoFile(maxSizeInMB: 200.0);

// Document files (default: 10MB)
RuleFor(x => x.DocumentFile).ValidDocumentFile();
RuleFor(x => x.DocumentFile).ValidDocumentFile(maxSizeInMB: 25.0);

// Any file type (default: 20MB)
RuleFor(x => x.AnyFile).ValidAnyFile();
RuleFor(x => x.AnyFile).ValidAnyFile(maxSizeInMB: 50.0);
```

### File Collections
```csharp
// Multiple files with type restriction
RuleFor(x => x.ImageGallery)
    .ValidFileCollection(FileType.Image, totalMaxSizeInMB: 100.0, maxFileCount: 20);

RuleFor(x => x.VideoLibrary)
    .ValidFileCollection(FileType.Video, totalMaxSizeInMB: 1000.0, maxFileCount: 5);

RuleFor(x => x.DocumentArchive)
    .ValidFileCollection(FileType.Document, totalMaxSizeInMB: 500.0, maxFileCount: 50);
```

## Real-World Examples - Simplified

### 1. User Profile Validator
```csharp
public class UserProfileValidator : AbstractValidator<UserProfileRequest>
{
    public UserProfileValidator()
    {
        RuleFor(x => x.AvatarImage)
            .ValidImageFile(maxSizeInMB: 2.0);
            
        RuleFor(x => x.CoverImage)
            .ValidImageFile(maxSizeInMB: 5.0);
    }
}
```

### 2. Product Validator
```csharp
public class ProductValidator : AbstractValidator<ProductRequest>
{
    public ProductValidator()
    {
        RuleFor(x => x.MainImage)
            .NotNull().WithMessage("Main product image is required")
            .ValidImageFile(maxSizeInMB: 10.0);
            
        RuleFor(x => x.GalleryImages)
            .ValidFileCollection(FileType.Image, totalMaxSizeInMB: 100.0, maxFileCount: 20);
            
        RuleFor(x => x.ManualFile)
            .ValidDocumentFile(maxSizeInMB: 20.0);
    }
}
```

### 3. Media Upload Validator
```csharp
public class MediaUploadValidator : AbstractValidator<MediaUploadRequest>
{
    public MediaUploadValidator()
    {
        RuleFor(x => x.ProfileImage)
            .ValidFile(FileType.Image, maxSizeInMB: 5.0);
            
        RuleFor(x => x.PromotionalVideo)
            .ValidFile(FileType.Video, maxSizeInMB: 200.0);
            
        RuleFor(x => x.Document)
            .ValidFile(FileType.Document, maxSizeInMB: 25.0);
            
        RuleFor(x => x.AnyFile)
            .ValidFile(FileType.All, maxSizeInMB: 50.0);
    }
}
```

### 4. Payment Method Validator (Updated)
```csharp
public class PaymentMethodValidator : AbstractValidator<PaymentMethodRequest>
{
    public PaymentMethodValidator()
    {
        RuleFor(x => x.IconImage)
            .ValidPaymentMethodIcon(maxSizeInMB: 3.0); // Business-specific method
            
        // Other validation rules...
    }
}
```

## File Type Reference

### Supported Extensions

| FileType | Extensions | Default Size | Use Case |
|----------|------------|--------------|----------|
| **Image** | jpg, jpeg, png, gif, webp, bmp, svg | 5MB | Photos, logos, icons |
| **Video** | mp4, avi, mov, wmv, flv, webm, mkv | 100MB | Promotional videos, demos |
| **Document** | pdf, doc, docx, txt, xlsx, xls, ppt, pptx, csv | 10MB | Contracts, reports, manuals |
| **All** | Any extension | 20MB | Archives, unknown types |

## API Reference

### Core Methods
```csharp
// Main flexible method
.ValidFile(FileType.Image, maxSizeInMB: 5.0)

// Convenience methods with defaults
.ValidImageFile(maxSizeInMB: 5.0)     // Images only
.ValidVideoFile(maxSizeInMB: 100.0)   // Videos only  
.ValidDocumentFile(maxSizeInMB: 10.0) // Documents only
.ValidAnyFile(maxSizeInMB: 20.0)      // Any file type

// File collections
.ValidFileCollection(FileType.Image, totalMaxSizeInMB: 100.0, maxFileCount: 20)
```

## Benefits - Simplified Version

### ✅ **Ultra Simple API**
- **4 file types** cover 99% of use cases: Image, Video, Document, All
- **1 main method** handles everything: `ValidFile(FileType, maxSize)`
- **Convenience methods** for quick usage: `ValidImageFile(size)`

### ✅ **Flexible & Business-Focused**
- **Custom max size** per business requirement
- **Enum-based** for type safety and clarity
- **Default values** for common scenarios

### ✅ **Reduced Complexity**
- **347 lines → 175 lines** (50% reduction)
- **Multiple classes → Single enum** approach
- **Easy to understand** and maintain

### ✅ **Backward Compatible**
- **Same method names** for common cases
- **Flexible max size** parameter maintained
- **No breaking changes** for existing code

## Migration from Complex Version

```csharp
// Old complex way
RuleFor(x => x.File).ValidImageFile(ImageValidationSettings.Product);

// New simple way  
RuleFor(x => x.File).ValidImageFile(maxSizeInMB: 10.0);

// Or using enum approach
RuleFor(x => x.File).ValidFile(FileType.Image, maxSizeInMB: 10.0);
```

**Result: Same functionality, much simpler API!** 🎯
