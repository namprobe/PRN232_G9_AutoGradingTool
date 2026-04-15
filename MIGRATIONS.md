# Entity Framework Migrations Guide

## 🔥 New: Enhanced Migration Scripts

Scripts đã được cải tiến với khả năng:
- ✅ Tự động load từ nhiều file `.env` theo thứ tự ưu tiên
- ✅ Hỗ trợ chỉ định env file cụ thể
- ✅ Phù hợp cho nhiều môi trường (dev, staging, production)

**👉 Đọc hướng dẫn chi tiết tại: [scripts/migrations/README.md](scripts/migrations/README.md)**

## 🚀 Quick Start

### Windows (PowerShell)

```powershell
# Auto-load từ .env files (thứ tự ưu tiên: .env, .env.local, .env.{Environment}, .env.docker)
.\scripts\migrations\migrate.ps1 -Action list

# Sử dụng env file cụ thể
.\scripts\migrations\migrate.ps1 -Action list -EnvFile ".env.production"

# Tạo migration
.\scripts\migrations\migrate.ps1 -Action add -Name "AddUserTable" -Context main

# Apply migrations
.\scripts\migrations\migrate.ps1 -Action update -Context main

# Remove migration
.\scripts\migrations\migrate.ps1 -Action remove -Context main
```

### Linux / macOS (Bash)

```bash
# First time: Make executable
chmod +x scripts/migrations/migrate.sh

# Auto-load từ .env files
./scripts/migrations/migrate.sh list

# Sử dụng env file cụ thể
./scripts/migrations/migrate.sh list "" both ".env.production"

# Tạo migration
./scripts/migrations/migrate.sh add "AddUserTable" main

# Apply migrations
./scripts/migrations/migrate.sh update "" main

# Remove migration
./scripts/migrations/migrate.sh remove "" main
```

## 🌐 Environment Files Priority

Scripts tự động load các file env theo thứ tự (file sau override file trước):

1. `.env` - Base configuration
2. `.env.local` - Local overrides (gitignored)
3. `.env.{ASPNETCORE_ENVIRONMENT}` - Environment-specific
4. `.env.docker` - Docker-specific overrides

### Setup Environment Files

```bash
# Copy examples
cp .env.example .env
cp .env.local.example .env.local

# Edit with your settings
nano .env.local
```

### Sample .env.local

```env
# Your personal local database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_local;Username=myuser;Password=mypass;
ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_outerdb_local;Username=myuser;Password=mypass;
```

---

## Cách Thủ Công (Không khuyến nghị)

Nếu không muốn dùng script, bạn có thể chạy trực tiếp câu lệnh EF Core:

### Windows (PowerShell)

```powershell
# Load environment variables từ .env.docker
Get-Content .env.docker | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim().Trim('"').Trim("'")
        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    }
}

# Tạo migration cho PRN232_G9_AutoGradingToolDbContext (Main Database)
dotnet ef migrations add YourMigrationName --context PRN232_G9_AutoGradingToolDbContext --output-dir Migrations/PRN232_G9_AutoGradingTool --project src/PRN232_G9_AutoGradingTool.Infrastructure

# Tạo migration cho PRN232_G9_AutoGradingToolOuterDbContext (Outer Database)
dotnet ef migrations add YourMigrationName --context PRN232_G9_AutoGradingToolOuterDbContext --output-dir Migrations/PRN232_G9_AutoGradingToolOuter --project src/PRN232_G9_AutoGradingTool.Infrastructure
```

### Linux/macOS (Bash)

```bash
# Load environment variables từ .env.docker
export $(grep -v '^#' .env.docker | xargs)

# Tạo migration
dotnet ef migrations add YourMigrationName --context PRN232_G9_AutoGradingToolDbContext --output-dir Migrations/PRN232_G9_AutoGradingTool --project src/PRN232_G9_AutoGradingTool.Infrastructure
dotnet ef migrations add YourMigrationName --context PRN232_G9_AutoGradingToolOuterDbContext --output-dir Migrations/PRN232_G9_AutoGradingToolOuter --project src/PRN232_G9_AutoGradingTool.Infrastructure
```

---

## Database Context Information

### PRN232_G9_AutoGradingToolDbContext (Main Database)
- **Connection String**: `ConnectionStrings__DefaultConnection`
- **Database**: `PRN232_G9_AutoGradingTool_db`
- **Migrations Folder**: `Migrations/PRN232_G9_AutoGradingTool`
- **Chứa**: Users, Roles, Identity tables, Business entities

### PRN232_G9_AutoGradingToolOuterDbContext (Outer Database)
- **Connection String**: `ConnectionStrings__OuterDbConnection`
- **Database**: `PRN232_G9_AutoGradingTool_outerdb`
- **Migrations Folder**: `Migrations/PRN232_G9_AutoGradingToolOuter`
- **Chứa**: Hangfire, System logs, Background job data

---

## Troubleshooting

### 1. Connection Errors

**Lỗi**: `No such host is known` hoặc `Failed to connect to 127.0.0.1:5432`

**Nguyên nhân**: Connection string trong `.env.docker` trỏ đến `Host=postgres` (Docker hostname) nhưng đang chạy local.

**Giải pháp**:
- Scripts đã tự động xử lý bằng cách dùng `--force` flag cho remove command
- Nếu vẫn gặp vấn đề, tạo `.env.local` với localhost connection:
  ```env
  ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_db;Username=admin;Password=admin@123;
  ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_outerdb;Username=admin;Password=admin@123;
  ```

### 2. Permission Denied (Linux/macOS)

**Lỗi**: `Permission denied: ./scripts/migrate.sh`

**Giải pháp**:
```bash
chmod +x scripts/migrate.sh
```

### 3. Migration Already Applied to Database

**Lỗi**: Cannot remove migration because it has been applied to the database

**Giải pháp**: Scripts đã dùng `--force` flag tự động. Migration sẽ được remove khỏi code, nhưng bạn cần revert thủ công trong database nếu cần.

### 4. Build Errors

**Lỗi**: Build failed khi tạo migration

**Giải pháp**:
```powershell
# Build project trước
dotnet build src/PRN232_G9_AutoGradingTool.Infrastructure
dotnet build src/PRN232_G9_AutoGradingTool.API

# Sau đó chạy migration
.\scripts\migrate.ps1 -Action add -Name "YourMigration"
```

---

## Best Practices

### 1. Naming Conventions
- Sử dụng PascalCase: `AddUserProfile`, `UpdateOrderStatus`
- Descriptive names: `AddEmailVerificationToUsers` thay vì `UpdateUsers`
- Avoid generic names: `Update1`, `Fix`, `Changes`

### 2. Migration Organization
- **Main DB**: User authentication, business logic entities
- **Outer DB**: Infrastructure concerns (Hangfire, logs, caching tables)

### 3. Testing Migrations
```powershell
# Test trên database mới
.\scripts\migrate.ps1 -Action add -Name "TestFeature" -Context main
.\scripts\migrate.ps1 -Action update -Context main

# Nếu có vấn đề, rollback
.\scripts\migrate.ps1 -Action remove -Context main
```

### 4. Production Deployment
- Review migration code trước khi deploy
- Backup database trước khi apply migrations
- Test trên staging environment trước
- Monitor logs khi apply migrations

### 5. Version Control
```bash
# Commit migrations cùng với code changes
git add src/PRN232_G9_AutoGradingTool.Infrastructure/Migrations/
git commit -m "feat: Add user profile migration"
```

---

## Lưu ý

1. **Connection Strings Priority**: 
   - Ưu tiên đọc từ Environment Variables trước
   - Fallback về appsettings.json nếu không có trong env

2. **Environment Variables Format**: 
   - Main DB: `ConnectionStrings__DefaultConnection`
   - Outer DB: `ConnectionStrings__OuterDbConnection`

3. **Docker**: 
   - Migrations được apply tự động khi container start (nếu có cấu hình trong startup code)
   - Connection strings sẽ được load từ `.env.docker` đã mount vào container

4. **Migration Assembly**: 
   - Đã cấu hình migrations lưu vào Infrastructure project
   - Retry logic đã được enable cho cả 2 DbContext

5. **Force Flag**:
   - Remove command tự động dùng `--force` để xóa migration files
   - Không cần kết nối database để remove migrations

---

## Quick Reference

📖 **[scripts/migrations/README.md](scripts/migrations/README.md)** - Đầy đủ hướng dẫn và examples

📑 **[MIGRATION-CHEATSHEET.md](MIGRATION-CHEATSHEET.md)** - Quick command reference

📁 **Script Files**:
- Windows: `scripts/migrations/migrate.ps1`
- Linux/macOS: `scripts/migrations/migrate.sh`

🔧 **Factory Files**:
- [PRN232_G9_AutoGradingToolDbContextFactory.cs](src/PRN232_G9_AutoGradingTool.Infrastructure/Context/PRN232_G9_AutoGradingToolDbContextFactory.cs)
- [PRN232_G9_AutoGradingToolOuterDbContextFactory.cs](src/PRN232_G9_AutoGradingTool.Infrastructure/Context/PRN232_G9_AutoGradingToolOuterDbContextFactory.cs)

🌐 **Environment Files** (gitignore `.env.local`):
- `.env.example` - Base template
- `.env.local.example` - Local template
- `.env` - Committed defaults
- `.env.local` - Your personal settings (gitignored)
- `.env.docker` - Docker overrides
