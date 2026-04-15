# Auth Validation Framework

Giải pháp tái sử dụng validation chung cho các use case authentication trong PRN232_G9_AutoGradingTool.

## Tổng quan

Framework này được thiết kế để giảm thiểu việc lặp lại code validation và đảm bảo tính nhất quán trong việc validate dữ liệu auth across tất cả các use cases.

## Kiến trúc

### 1. AuthValidationExtensions.cs
Extension methods cho FluentValidation cung cấp các validation rules chung:

#### Email Validation
- `ValidEmail()` - Validate format email cơ bản
- `ValidEmailExists()` - Validate email format + kiểm tra email tồn tại trong DB
- `ValidEmailUnique()` - Validate email format + kiểm tra email chưa tồn tại (dành cho đăng ký)

#### Password Validation
- `ValidPassword(minLength)` - Validate password với độ dài tối thiểu
- `ValidConfirmPassword(passwordSelector)` - Validate confirm password khớp với password

#### Phone Number Validation
- `ValidPhoneNumber()` - Validate format số điện thoại
- `ValidPhoneNumberUnique()` - Validate phone format + kiểm tra số phone chưa tồn tại

#### Contact Validation (Dynamic)
- `ValidContactByChannel(channelSelector)` - Validate contact dựa trên channel (Email/SMS)
- `ValidContactExistsByChannel(channelSelector, unitOfWork)` - Validate contact + kiểm tra tồn tại

#### Other Validations
- `ValidPersonName(fieldName, maxLength)` - Validate tên người (firstName, lastName)
- `ValidOtp()` - Validate OTP code (6 digits)

### 2. BaseAuthValidator.cs
Abstract base class cho tất cả auth validators:
- Cung cấp dependency injection cho IUnitOfWork
- Template method pattern với `SetupValidationRules()`
- Đảm bảo tính nhất quán trong cách implement validators

## Cách sử dụng

### Tạo validator mới
```csharp
public class MyAuthCommandValidator : BaseAuthValidator<MyAuthCommand>
{
    public MyAuthCommandValidator(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        SetupValidationRules();
    }

    protected override void SetupValidationRules()
    {
        RuleFor(x => x.Request.Email)
            .ValidEmailUnique(UnitOfWork);

        RuleFor(x => x.Request.Password)
            .ValidPassword(8);

        RuleFor(x => x.Request.Contact)
            .ValidContactExistsByChannel(x => x.Request.OtpChannel, UnitOfWork);
    }
}
```

### Các validator đã được refactor
1. **LoginCommandValidator** - Sử dụng `ValidEmail()`
2. **RegisterCommandValidator** - Sử dụng `ValidEmailUnique()`, `ValidPassword()`, `ValidConfirmPassword()`, `ValidPersonName()`, `ValidPhoneNumberUnique()`
3. **VerifyOtpCommandValidator** - Sử dụng `ValidContactByChannelOnly()`, `ValidOtp()`
4. **ResetPasswordCommandValidator** - Sử dụng `ValidContactExistsByChannel()`, `ValidPassword()`

**Lưu ý:** 
- `VerifyOtpCommandValidator` sử dụng `ValidContactByChannelOnly()` vì user data có thể đang cache.
- `ResetPasswordCommandValidator` sử dụng `ValidContactExistsByChannel()` để early validation và UX tốt hơn.
- Database queries đã được tối ưu để giảm số lần truy vấn.

## Lợi ích

### 1. DRY Principle
- Loại bỏ code duplicate trong các validators
- Một lần implement, sử dụng nhiều nơi

### 2. Consistency
- Đảm bảo các validation rules giống nhau across tất cả use cases
- Cùng error messages, cùng logic validation

### 3. Maintainability
- Thay đổi validation logic ở một nơi, apply cho tất cả
- Dễ dàng thêm validation rules mới

### 4. Testability
- Extension methods có thể test độc lập
- Base validator cung cấp structure rõ ràng cho unit tests

### 5. Type Safety
- Sử dụng generic types và expressions
- Compile-time checking cho validation rules

### 6. Performance Optimization
- **Giảm database queries**: Từ 3 queries xuống 1-2 queries cho password reset flow
- **Smart validation strategy**: Validate tại đúng thời điểm (early vs lazy)
- **AsNoTracking queries**: Sử dụng cho read-only operations
- **Single tracked query**: Chỉ 1 query tracked cho update operations
- **Short-circuit validation**: Format validation trước, DB query sau - dừng ngay khi format fail
- **Conditional database access**: Chỉ query database khi format validation pass

## Ví dụ thực tế

### Short-circuit Validation Performance
```csharp
// ValidContactExistsByChannel với short-circuit optimization
RuleFor(x => x.Request.Contact)
    .ValidContactExistsByChannel(x => x.Request.OtpSentChannel, UnitOfWork);

// Flow:
// 1. Check format first (in-memory, fast)
// 2. If format invalid → return error immediately (no DB query)
// 3. If format valid → query database to check existence
// 4. Return appropriate error message based on validation step
```

### Validation Contact dựa trên Channel
```csharp
// Trước khi refactor
RuleFor(x => x.Request.Contact)
    .Must((command, contact) => {
        return command.Request.OtpSentChannel switch
        {
            NotificationChannelEnum.Email => IsValidEmail(contact),
            NotificationChannelEnum.SMS => IsValidPhoneNumber(contact),
            _ => false
        };
    })
    .WithMessage("Invalid contact format");

// Sau khi refactor
RuleFor(x => x.Request.Contact)
    .ValidContactExistsByChannel(x => x.Request.OtpSentChannel, UnitOfWork);
```

### Validation Email unique
```csharp
// Trước khi refactor
RuleFor(x => x.Request.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MustAsync(async (email, cancellationToken) => 
        !await _unitOfWork.Repository<AppUser>().AnyAsync(x => x.Email == email))
    .WithMessage("Email already exists");

// Sau khi refactor
RuleFor(x => x.Request.Email)
    .ValidEmailUnique(UnitOfWork);
```

## Extensibility

Framework được thiết kế để dễ dàng mở rộng:

1. **Thêm validation rules mới** - Tạo extension methods trong `AuthValidationExtensions`
2. **Custom error messages** - Override messages trong validators cụ thể
3. **Complex validations** - Combine nhiều extension methods
4. **Domain-specific validations** - Tạo domain-specific extensions

## Best Practices

1. **Sử dụng BaseAuthValidator** cho tất cả auth-related validators
2. **Gọi SetupValidationRules()** trong constructor
3. **Sử dụng UnitOfWork** thay vì inject repository trực tiếp
4. **Combine extension methods** thay vì viết logic validation phức tạp
5. **Đặt tên descriptive** cho validation methods
6. **Performance và tracking optimization:**
   - Sử dụng `ValidContactByChannelOnly()` cho cached data scenarios (VerifyOTP)
   - Sử dụng `ValidContactExistsByChannel()` cho early user feedback (ResetPassword)
   - Single tracked query trong update operations để tránh tracking conflicts
   - AsNoTracking cho read-only validation queries
7. **Separation of concerns:**
   - Validators: Format validation và early business rule checks
   - Handlers: Complex business logic và database update operations
8. **Query optimization strategy:**
   - Early validation in request phase (ResetPassword)
   - Lazy validation in execution phase (VerifyOTP)
   - Minimize redundant database calls across the flow
