# Migration Scripts

Công cụ quản lý Entity Framework migrations cho PRN232_G9_AutoGradingTool với hỗ trợ đa môi trường.

## 🌟 Tính Năng

- ✅ **Đa môi trường**: Tự động load từ nhiều file `.env` theo thứ tự ưu tiên
- ✅ **Cross-platform**: Hỗ trợ Windows (PowerShell) và Linux/macOS (Bash)
- ✅ **Dual Database**: Quản lý 2 databases độc lập (main & outer)
- ✅ **Auto-detection**: Tự động phát hiện và load environment files
- ✅ **Custom env file**: Chỉ định file env cụ thể khi cần

## 🚀 Quick Start

### Windows (PowerShell)

```powershell
# Basic usage - auto-load từ .env files
.\scripts\migrations\migrate.ps1 -Action list

# Specify custom env file
.\scripts\migrations\migrate.ps1 -Action list -EnvFile ".env.production"

# Create migration
.\scripts\migrations\migrate.ps1 -Action add -Name "AddUserTable" -Context main

# Apply migrations
.\scripts\migrations\migrate.ps1 -Action update -Context main

# Remove last migration
.\scripts\migrations\migrate.ps1 -Action remove -Context main
```

### Linux / macOS (Bash)

```bash
# First time: Make executable
chmod +x scripts/migrations/migrate.sh

# Basic usage - auto-load từ .env files
./scripts/migrations/migrate.sh list

# Specify custom env file
./scripts/migrations/migrate.sh list "" both ".env.production"

# Create migration
./scripts/migrations/migrate.sh add "AddUserTable" main

# Apply migrations
./scripts/migrations/migrate.sh update "" main

# Remove last migration
./scripts/migrations/migrate.sh remove "" main
```

## 📁 Environment Files Priority

Scripts tự động load các file env theo thứ tự (file sau override file trước):

1. `.env` - Base configuration
2. `.env.local` - Local overrides
3. `.env.{ASPNETCORE_ENVIRONMENT}` - Environment-specific (Development, Staging, Production)
4. `.env.docker` - Docker-specific overrides

### Ví dụ Environment File Structure

```
project-root/
├── .env                 # Base config (committed to git)
├── .env.local          # Local development (gitignored)
├── .env.Development    # Development environment
├── .env.Staging        # Staging environment
├── .env.Production     # Production environment
└── .env.docker         # Docker overrides
```

### Sample .env Files

**`.env` (Base)**
```env
# Default settings
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_db;Username=postgres;Password=postgres;
ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_outerdb;Username=postgres;Password=postgres;
```

**`.env.docker` (Docker Override)**
```env
# Docker uses service names instead of localhost
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=PRN232_G9_AutoGradingTool_db;Username=admin;Password=admin@123;
ConnectionStrings__OuterDbConnection=Host=postgres;Port=5432;Database=PRN232_G9_AutoGradingTool_outerdb;Username=admin;Password=admin@123;
```

**`.env.local` (Local Development Override - gitignored)**
```env
# Your personal local settings
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=PRN232_G9_AutoGradingTool_dev;Username=myuser;Password=mypass;
```

## 📋 Parameters

### PowerShell (migrate.ps1)

| Parameter | Required | Default | Values | Description |
|-----------|----------|---------|--------|-------------|
| `-Action` | Yes | - | `add`, `update`, `remove`, `list` | Migration action |
| `-Name` | For `add` | - | String | Migration name |
| `-Context` | No | `both` | `both`, `main`, `outer` | Database context |
| `-EnvFile` | No | Auto | Path | Specific env file to load |

### Bash (migrate.sh)

| Position | Required | Default | Values | Description |
|----------|----------|---------|--------|-------------|
| `$1` | Yes | - | `add`, `update`, `remove`, `list` | Migration action |
| `$2` | For `add` | - | String | Migration name (use `""` for others) |
| `$3` | No | `both` | `both`, `main`, `outer` | Database context |
| `$4` | No | Auto | Path | Specific env file to load |

## 🗄️ Database Contexts

| Context | Database | Migrations Folder | Contains |
|---------|----------|-------------------|----------|
| `main` | `PRN232_G9_AutoGradingTool_db` | `Migrations/PRN232_G9_AutoGradingTool` | Users, Roles, Identity, Business entities |
| `outer` | `PRN232_G9_AutoGradingTool_outerdb` | `Migrations/PRN232_G9_AutoGradingToolOuter` | Hangfire, System logs, Background jobs |
| `both` | Both databases | Both folders | *Default if not specified* |

## 💡 Common Use Cases

### Development on Local Machine

```powershell
# Windows: Create .env.local with localhost settings
# Then run migrations
.\scripts\migrations\migrate.ps1 -Action update
```

```bash
# Linux/macOS
./scripts/migrations/migrate.sh update
```

### Development with Docker

```powershell
# Windows: Use docker env explicitly
.\scripts\migrations\migrate.ps1 -Action update -EnvFile ".env.docker"
```

```bash
# Linux/macOS: Use docker env explicitly
./scripts/migrations/migrate.sh update "" both ".env.docker"
```

### Production Deployment

```powershell
# Windows: Use production env
.\scripts\migrations\migrate.ps1 -Action update -EnvFile ".env.Production"
```

```bash
# Linux/macOS: Use production env
./scripts/migrations/migrate.sh update "" both ".env.Production"
```

### Create Environment-Specific Migration

```powershell
# Development
$env:ASPNETCORE_ENVIRONMENT="Development"
.\scripts\migrations\migrate.ps1 -Action add -Name "AddDevFeature" -Context main

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"
.\scripts\migrations\migrate.ps1 -Action add -Name "AddProdFeature" -Context main
```

## 🔧 Troubleshooting

### Connection String Not Found

**Problem**: `[WARN] DefaultConnection not found in environment`

**Solution**: 
1. Verify your `.env` files exist and contain correct variables
2. Check variable format: `ConnectionStrings__DefaultConnection=...`
3. Try specifying env file explicitly: `-EnvFile ".env.docker"`

### Permission Denied (Linux/macOS)

**Problem**: `Permission denied: ./scripts/migrations/migrate.sh`

**Solution**:
```bash
chmod +x scripts/migrations/migrate.sh
```

### Migration Already Applied

**Problem**: Cannot remove migration because it's applied to database

**Solution**: Scripts use `--force` flag automatically to remove from code. Database changes need manual revert if needed.

## 📚 Additional Documentation

- 📖 [Full Migration Guide](../../MIGRATIONS.md) - Detailed documentation
- 📋 [Migration Cheatsheet](../../MIGRATION-CHEATSHEET.md) - Quick command reference
- 🏗️ [DbContext Factories](../../src/PRN232_G9_AutoGradingTool.Infrastructure/Context/) - Factory implementations

## 🎯 Best Practices

1. **Version Control**
   - Commit `.env` with safe defaults
   - Add `.env.local` to `.gitignore`
   - Never commit sensitive credentials

2. **Environment Management**
   - Use `.env.local` for personal settings
   - Use `.env.{Environment}` for team environments
   - Use `.env.docker` for container-specific settings

3. **Migration Naming**
   - Use descriptive names: `AddUserProfileFields`
   - Use PascalCase convention
   - Avoid generic names like `Update1`, `Fix`

4. **Testing**
   - Test migrations on dev database first
   - Review generated migration code
   - Keep backups before applying to production

## 🔗 Related Scripts

- **demo.ps1** / **demo.sh** - Interactive demo of all features
- **Old scripts/** - Legacy scripts (deprecated, use `migrations/` instead)

## 📝 Examples

### Standard Workflow

```powershell
# 1. Create migration
.\scripts\migrations\migrate.ps1 -Action add -Name "AddEmailVerification" -Context main

# 2. Review generated files
# Check: src/PRN232_G9_AutoGradingTool.Infrastructure/Migrations/PRN232_G9_AutoGradingTool/

# 3. Apply to database
.\scripts\migrations\migrate.ps1 -Action update -Context main

# 4. Verify
.\scripts\migrations\migrate.ps1 -Action list -Context main
```

### Multi-Environment Workflow

```powershell
# Development
$env:ASPNETCORE_ENVIRONMENT="Development"
.\scripts\migrations\migrate.ps1 -Action update

# Staging
$env:ASPNETCORE_ENVIRONMENT="Staging"
.\scripts\migrations\migrate.ps1 -Action update

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"
.\scripts\migrations\migrate.ps1 -Action update -EnvFile ".env.Production"
```
