# Multilingual Support - Adding New Languages

This guide explains how to add support for additional languages to the PRN232_G9_AutoGradingTool API.

## Current Implementation

The application currently supports:
- **English (en)** - Default fallback language
- **Vietnamese (vi)** - Fully translated

## Architecture Overview

The multilingual system uses Microsoft's built-in localization with:
- **Resource files (.resx)** for storing translations
- **IStringLocalizer** for runtime localization
- **Three-level fallback mechanism** (Requested → Default → Error Code)
- **Accept-Language header** for language detection

## How to Add a New Language

Adding support for a new language (e.g., Thai, Indonesian, Chinese) is a simple 3-step process:

### Step 1: Create New Resource Files

Create new `.resx` files for your target language using the appropriate culture code:

```
PRN232_G9_AutoGradingTool.Application/Resources/
  ├── ErrorMessages.resx           (English - default)
  ├── ErrorMessages.vi.resx        (Vietnamese)
  ├── ErrorMessages.th.resx        ← NEW: Thai
  ├── ErrorMessages.id.resx        ← NEW: Indonesian
  ├── ErrorMessages.zh.resx        ← NEW: Chinese
  │
  ├── SuccessMessages.resx
  ├── SuccessMessages.th.resx      ← NEW
  ├── SuccessMessages.id.resx      ← NEW
  │
  ├── ValidationMessages.resx
  ├── ValidationMessages.th.resx   ← NEW
  ├── ValidationMessages.id.resx   ← NEW
  │
  ├── CommonMessages.resx
  ├── CommonMessages.th.resx       ← NEW
  └── CommonMessages.id.resx       ← NEW
```

**Culture Code Reference:**
- `th` - Thai
- `id` - Indonesian  
- `zh` - Chinese (Simplified)
- `zh-TW` - Chinese (Traditional)
- `ja` - Japanese
- `ko` - Korean
- `fr` - French
- `de` - German
- `es` - Spanish

### Step 2: Translate All Resource Keys

Copy the structure from the English `.resx` files and translate each value:

**Example: ErrorMessages.th.resx (Thai)**
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!-- Copy the XML schema from ErrorMessages.resx -->
  
  <data name="ErrorCode_Unauthorized" xml:space="preserve">
    <value>จำเป็นต้องตรวจสอบสิทธิ์</value>
  </data>
  <data name="ErrorCode_Forbidden" xml:space="preserve">
    <value>การเข้าถึงถูกปฏิเสธ</value>
  </data>
  <data name="ErrorCode_ValidationFailed" xml:space="preserve">
    <value>การตรวจสอบล้มเหลว</value>
  </data>
  <!-- ... translate all other keys ... -->
</root>
```

**Important:** Make sure to:
- Keep all resource keys identical to the English version
- Only translate the `<value>` content
- Preserve placeholders like `{0}`, `{PropertyName}`, `{MinLength}` etc.

### Step 3: Update Supported Cultures Configuration

In `PRN232_G9_AutoGradingTool.API/Configurations/ServiceConfiguration.cs`, add your new language code to the `supportedCultures` array:

```csharp
public static WebApplication ConfigurePipeline(this WebApplication app)
{
    // Simply add new culture codes to this array
    var supportedCultures = new[] { "en", "vi", "th", "id", "zh" };
    
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    
    // ... rest of configuration
}
```

**That's it!** No other code changes are needed.

## Testing Your New Language

Test your new language using the `Accept-Language` HTTP header:

```bash
# Test with Thai
curl -H "Accept-Language: th" http://localhost:5000/api/auth/login

# Test with Indonesian
curl -H "Accept-Language: id" http://localhost:5000/api/users

# Test with Chinese
curl -H "Accept-Language: zh" http://localhost:5000/api/documents
```

## Resource File Categories

### ErrorMessages.resx
Contains all error code messages and generic exception messages:
- `ErrorCode_*` - Maps to ErrorCodeEnum values
- `Exception_*` - Generic exception messages for various exception types

**Total keys:** ~45 keys

### SuccessMessages.resx
Contains success operation messages:
- `Success_Default` - Generic success
- `Success_Created` - Resource created
- `Success_Updated` - Resource updated
- `Success_User_Created`, etc. - Specific operations

**Total keys:** ~12 keys

### ValidationMessages.resx
Contains FluentValidation messages:
- Built-in validators: `NotEmpty`, `EmailAddress`, `MinimumLength`, etc.
- Custom messages: `Email_Required`, `Password_TooShort`, etc.

**Total keys:** ~15 keys

### CommonMessages.resx
Contains common UI messages:
- `Pagination_NoResults`
- `Pagination_TotalResults`
- `Loading`, `Processing`, etc.

**Total keys:** ~6 keys

## Fallback Mechanism

The system uses a three-level fallback:

1. **Level 1:** Try to get resource from requested culture (e.g., `th`)
2. **Level 2:** If not found, fall back to default culture (`en`)
3. **Level 3:** If still not found, return error code/key as string

This ensures the API never crashes due to missing translations.

## Best Practices

### 1. Complete Translation
Translate **all** resource keys for the best user experience. Incomplete translations will fall back to English.

### 2. Maintain Placeholders
When translating, preserve placeholder syntax:

```xml
<!-- English -->
<value>'{PropertyName}' must be at least {MinLength} characters</value>

<!-- Thai - Keep placeholders! -->
<value>'{PropertyName}' ต้องมีอย่างน้อย {MinLength} ตัวอักษร</value>
```

### 3. Test All Error Scenarios
Test your translations across:
- Validation errors
- Authentication errors
- Database errors
- File operation errors
- Business logic errors

### 4. Cultural Considerations
Consider cultural differences in:
- Formal vs. informal language
- Date/time formats (handled by .NET's CultureInfo)
- Number formats (handled by .NET's CultureInfo)
- Error message tone and politeness level

## Example: Adding Indonesian Support

Here's a complete example of adding Indonesian (id) support:

1. **Create 4 new files:**
   - `ErrorMessages.id.resx`
   - `SuccessMessages.id.resx`
   - `ValidationMessages.id.resx`
   - `CommonMessages.id.resx`

2. **Translate error messages:**
```xml
<!-- ErrorMessages.id.resx -->
<data name="ErrorCode_Unauthorized" xml:space="preserve">
  <value>Autentikasi diperlukan</value>
</data>
<data name="ErrorCode_ValidationFailed" xml:space="preserve">
  <value>Validasi gagal</value>
</data>
<!-- ... etc -->
```

3. **Update configuration:**
```csharp
var supportedCultures = new[] { "en", "vi", "id" };
```

4. **Test:**
```bash
curl -H "Accept-Language: id" http://localhost:5000/api/auth/login
```

## Troubleshooting

### Issue: Translation not appearing
**Solution:** Check that:
1. Culture code matches exactly (case-sensitive)
2. Resource key matches exactly (case-sensitive)
3. File is named correctly: `ResourceName.{culture}.resx`
4. Culture is added to `supportedCultures` array

### Issue: Seeing English instead of my language
**Solution:** Check:
1. `Accept-Language` header is set correctly
2. Culture code is supported (in `supportedCultures`)
3. Resource file exists and contains the key

### Issue: Build errors after adding resource files
**Solution:**
- Ensure XML structure is valid
- Check for encoding issues (file should be UTF-8)
- Verify all XML tags are properly closed

## Monitoring Translation Coverage

The application logs warnings when translations are missing:

```
Warning: Missing localization for key: ErrorCode_NewErrorCode, Culture: th
```

Monitor these logs to identify gaps in your translations.

## Getting Translation Data

For professional translations, export your resource keys to CSV:

```csharp
// Helper script to generate CSV for translators
var keys = new[] { 
    "ErrorCode_Unauthorized",
    "ErrorCode_Forbidden",
    // ... all keys
};

var csv = string.Join("\n", keys.Select(k => $"{k},<ENGLISH_VALUE>,<TRANSLATION_NEEDED>"));
File.WriteAllText("translation-template.csv", csv);
```

Then import translated CSV back into `.resx` files.

## Summary

Adding a new language requires:
✅ Create 4 `.resx` files with translations  
✅ Add culture code to configuration (1 line)  
✅ Test with `Accept-Language` header

**No code changes required!** The localization infrastructure handles everything automatically.

---

For questions or issues, contact the development team.
