# Localization Mapping Guide

Tài liệu này giải thích cách thức localization map giữa **ErrorCodeEnum**, **Exception**, và **Resource Keys**.

## 📋 Tổng quan

Hệ thống localization sử dụng 3 loại resource files:
1. **ErrorMessages.resx** - Error codes và exception messages
2. **SuccessMessages.resx** - Success messages
3. **ValidationMessages.resx** - Validation messages
4. **CommonMessages.resx** - Common UI messages

## 🔑 Mapping Rules

### 1. ErrorCodeEnum → Resource Key

**Format:** `ErrorCode_{ErrorCodeEnum}`

**Ví dụ:**
```csharp
ErrorCodeEnum.Unauthorized → "ErrorCode_Unauthorized"
ErrorCodeEnum.InvalidCredentials → "ErrorCode_InvalidCredentials"
ErrorCodeEnum.ValidationFailed → "ErrorCode_ValidationFailed"
```

**Implementation:**
```csharp
// Trong LocalizationService.cs
public string GetErrorMessage(ErrorCodeEnum errorCode)
{
    var key = $"ErrorCode_{errorCode}";  // ErrorCode_Unauthorized
    return GetLocalizedString(_errorLocalizer, key, errorCode.ToString());
}
```

**Resource File:**
```xml
<!-- ErrorMessages.resx -->
<data name="ErrorCode_Unauthorized" xml:space="preserve">
    <value>Authentication required</value>
</data>

<!-- ErrorMessages.vi.resx -->
<data name="ErrorCode_Unauthorized" xml:space="preserve">
    <value>Yêu cầu xác thực</value>
</data>
```

### 2. Exception Type → Resource Key

**Format:** `Exception_{ExceptionName}` (bỏ suffix "Exception" nếu có)

**Ví dụ:**
```csharp
UnauthorizedException → "Exception_Unauthorized"
InvalidTokenException → "Exception_InvalidToken"
ForbiddenAccessException → "Exception_ForbiddenAccess"
UserNotFoundException → "Exception_UserNotFound"
```

**Implementation:**
```csharp
// Sử dụng extension method
var exceptionType = typeof(UnauthorizedException);
var key = exceptionType.ToExceptionKey();  // "Exception_Unauthorized"

// Hoặc manual
var exceptionName = exceptionType.Name;  // "UnauthorizedException"
if (exceptionName.EndsWith("Exception"))
{
    exceptionName = exceptionName.Substring(0, exceptionName.Length - "Exception".Length);
}
var key = $"Exception_{exceptionName}";  // "Exception_Unauthorized"
```

**Resource File:**
```xml
<!-- ErrorMessages.resx -->
<data name="Exception_UserNotFound" xml:space="preserve">
    <value>User not found</value>
</data>

<!-- ErrorMessages.vi.resx -->
<data name="Exception_UserNotFound" xml:space="preserve">
    <value>Không tìm thấy người dùng</value>
</data>
```

### 3. Success Messages → Resource Key

**Format:** `Success_{Key}`

**Ví dụ:**
```csharp
"Success_Login" → "Đăng nhập thành công" (vi) / "Login successful" (en)
"Success_Logout" → "Đăng xuất thành công" (vi) / "Logout successful" (en)
"Success_RefreshToken" → "Làm mới token thành công" (vi) / "Token refreshed successfully" (en)
```

**Implementation:**
```csharp
_localizationService.GetSuccessMessage("Success_Login");
```

### 4. Validation Messages → Resource Key

**Format:** `{Key}` (không có prefix)

**Ví dụ:**
```csharp
"Email_Required" → "Email là bắt buộc" (vi) / "Email is required" (en)
"Password_Required" → "Mật khẩu là bắt buộc" (vi) / "Password is required" (en)
"NotEmpty" → "'{PropertyName}' không được để trống" (vi) / "'{PropertyName}' must not be empty" (en)
```

**Implementation:**
```csharp
_localizationService.GetValidationMessage("Email_Required");
```

## 🔄 Conversion Methods

### ErrorCodeEnum to Key

```csharp
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;

// PascalCase (default)
var key = ErrorCodeEnum.Unauthorized.ToErrorCodeKey();
// Result: "ErrorCode_Unauthorized"

// Snake_case (nếu cần)
var keySnake = ErrorCodeEnum.InvalidCredentials.ToErrorCodeKeySnakeCase();
// Result: "ErrorCode_invalid_credentials"
```

### Exception Type to Key

```csharp
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;

var exceptionType = typeof(UnauthorizedException);

// PascalCase (default)
var key = exceptionType.ToExceptionKey();
// Result: "Exception_Unauthorized"

// Snake_case (nếu cần)
var keySnake = exceptionType.ToExceptionKeySnakeCase();
// Result: "Exception_unauthorized"
```

### String Conversion

```csharp
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;

// PascalCase → snake_case
var snake = "InvalidCredentials".ToSnakeCase();
// Result: "invalid_credentials"

// PascalCase → camelCase
var camel = "InvalidCredentials".ToCamelCase();
// Result: "invalidCredentials"
```

## 📊 Mapping Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    Application Code                      │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   ErrorCodeEnum.Unauthorized   │
        └───────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   ToErrorCodeKey()             │
        │   → "ErrorCode_Unauthorized"   │
        └───────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   LocalizationService          │
        │   GetErrorMessage()            │
        └───────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   IStringLocalizer             │
        │   ErrorMessages resource       │
        └───────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   ErrorMessages.vi.resx         │
        │   <data name="ErrorCode_...">  │
        └───────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   "Yêu cầu xác thực"           │
        └───────────────────────────────┘
```

## 🎯 Usage Examples

### Example 1: ErrorCodeEnum Mapping

```csharp
// In Handler
return Result<AuthResponse>.Failure(
    _localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized), 
    ErrorCodeEnum.Unauthorized);

// Flow:
// ErrorCodeEnum.Unauthorized 
// → "ErrorCode_Unauthorized" (key)
// → "Yêu cầu xác thực" (vi) / "Authentication required" (en)
```

### Example 2: Exception Mapping

```csharp
// In Middleware
throw new UnauthorizedAccessException();

// In GlobalExceptionHandlingMiddleware
if (exception is UnauthorizedAccessException)
{
    var errorCode = ErrorCodeEnum.Unauthorized;
    var message = GetLocalizedErrorMessage(errorCode); // Uses ErrorCodeEnum
    // ErrorCodeEnum.Unauthorized → "ErrorCode_Unauthorized" → localized message
}
```

### Example 3: Custom Exception Key

```csharp
// If you need custom exception key
var key = typeof(MyCustomException).ToExceptionKey();
// Result: "Exception_MyCustom"

// Then in resource file:
<data name="Exception_MyCustom" xml:space="preserve">
    <value>Custom error message</value>
</data>
```

## 📝 Naming Conventions

### Current Convention (PascalCase)

**ErrorCodeEnum:**
- `Unauthorized` → `ErrorCode_Unauthorized`
- `InvalidCredentials` → `ErrorCode_InvalidCredentials`

**Exception:**
- `InvalidTokenException` → `Exception_InvalidToken`

**Success:**
- `Success_Login`
- `Success_RefreshToken`

**Validation:**
- `Email_Required`
- `Password_Required`

### Alternative: Snake_case (nếu cần)

Nếu muốn sử dụng snake_case cho consistency:

```csharp
// ErrorCodeEnum
ErrorCodeEnum.InvalidCredentials.ToErrorCodeKeySnakeCase();
// → "ErrorCode_invalid_credentials"

// Exception
typeof(InvalidTokenException).ToExceptionKeySnakeCase();
// → "Exception_invalid_token"
```

**Note:** Hiện tại hệ thống đang dùng **PascalCase** vì:
- ErrorCodeEnum values là PascalCase
- Exception class names là PascalCase
- Dễ đọc và maintain hơn

## 🔍 How to Find the Right Key

### For ErrorCodeEnum:

1. Look at `ErrorCodeEnum.cs`:
   ```csharp
   public enum ErrorCodeEnum
   {
       Unauthorized = 1001,
       InvalidCredentials = 1003,
       // ...
   }
   ```

2. Convert to key:
   ```csharp
   ErrorCodeEnum.Unauthorized → "ErrorCode_Unauthorized"
   ```

3. Check resource file:
   ```xml
   <data name="ErrorCode_Unauthorized" xml:space="preserve">
       <value>...</value>
   </data>
   ```

### For Exception:

1. Look at exception class name:
   ```csharp
   public class UnauthorizedException : Exception { }
   ```

2. Convert to key:
   ```csharp
   typeof(UnauthorizedException).ToExceptionKey()
   // → "Exception_Unauthorized"
   ```

3. Check resource file:
   ```xml
   <data name="Exception_Unauthorized" xml:space="preserve">
       <value>...</value>
   </data>
   ```

## ⚠️ Important Notes

1. **Case Sensitivity:** Resource keys are **case-sensitive**
   - ✅ `ErrorCode_Unauthorized`
   - ❌ `ErrorCode_unauthorized`
   - ❌ `errorcode_Unauthorized`

2. **Exact Match Required:** Key phải match chính xác
   - ✅ `ErrorCode_Unauthorized`
   - ❌ `ErrorCode_UnauthorizedAccess` (sai)

3. **Fallback Mechanism:**
   - Level 1: Requested culture (vi)
   - Level 2: Default culture (en)
   - Level 3: ErrorCodeEnum.ToString() hoặc key name

4. **No Snake_case by Default:**
   - Hệ thống hiện tại dùng PascalCase
   - Snake_case chỉ dùng khi cần convert manually
   - Resource files luôn dùng PascalCase keys

## 🛠️ Helper Methods

Sử dụng extension methods trong `LocalizationExtensions.cs`:

```csharp
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;

// ErrorCodeEnum → Key
var key1 = ErrorCodeEnum.Unauthorized.ToErrorCodeKey();
var key2 = ErrorCodeEnum.InvalidCredentials.ToErrorCodeKeySnakeCase();

// Exception → Key
var key3 = typeof(UnauthorizedException).ToExceptionKey();
var key4 = typeof(InvalidTokenException).ToExceptionKeySnakeCase();

// String conversion
var snake = "InvalidCredentials".ToSnakeCase();
var camel = "InvalidCredentials".ToCamelCase();
```

## 📚 Summary

| Source | Format | Example | Resource File |
|--------|--------|---------|---------------|
| `ErrorCodeEnum` | `ErrorCode_{Enum}` | `ErrorCode_Unauthorized` | ErrorMessages.resx |
| `Exception` | `Exception_{Name}` | `Exception_Unauthorized` | ErrorMessages.resx |
| Success | `Success_{Key}` | `Success_Login` | SuccessMessages.resx |
| Validation | `{Key}` | `Email_Required` | ValidationMessages.resx |

**Key Rule:** Tất cả keys đều dùng **PascalCase**, không phải snake_case!
