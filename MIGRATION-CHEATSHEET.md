# Migration Cheatsheet

## 🚀 Quick Commands

### Windows
```powershell
# List migrations
.\scripts\migrations\migrate.ps1 -Action list

# Add migration
.\scripts\migrations\migrate.ps1 -Action add -Name "MigrationName" -Context main

# Apply migrations
.\scripts\migrations\migrate.ps1 -Action update -Context main

# Remove last migration
.\scripts\migrations\migrate.ps1 -Action remove -Context main

# Use specific env file
.\scripts\migrations\migrate.ps1 -Action list -EnvFile ".env.production"
```

### Linux / macOS
```bash
# Make executable (first time only)
chmod +x scripts/migrations/migrate.sh

# List migrations
./scripts/migrations/migrate.sh list

# Add migration
./scripts/migrations/migrate.sh add "MigrationName" main

# Apply migrations
./scripts/migrations/migrate.sh update "" main

# Remove last migration
./scripts/migrations/migrate.sh remove "" main

# Use specific env file
./scripts/migrations/migrate.sh list "" both ".env.production"
```

## 🌍 Environment Files

Scripts auto-load in this priority (later overrides earlier):
1. `.env` - Base (committed)
2. `.env.local` - Personal (gitignored)
3. `.env.{ASPNETCORE_ENVIRONMENT}` - Environment-specific
4. `.env.docker` - Docker overrides

### Setup
```bash
# Copy templates
cp .env.example .env
cp .env.local.example .env.local

# Edit your local settings
nano .env.local
```

## 📋 Context Options

| Context | Database | Contains |
|---------|----------|----------|
| `main` | `PRN232_G9_AutoGradingTool_db` | Users, Roles, Identity, Business data |
| `outer` | `PRN232_G9_AutoGradingTool_outerdb` | Hangfire, System logs, Background jobs |
| `both` | Both databases | Default if not specified |

## 💡 Common Use Cases

```powershell
# Windows: Create user management migration
.\scripts\migrate.ps1 -Action add -Name "AddUserProfile" -Context main

# Linux/macOS: Create logging migration
./scripts/migrate.sh add "AddAuditLog" outer

# Apply all pending migrations
.\scripts\migrate.ps1 -Action update  # Windows
./scripts/migrate.sh update          # Linux/macOS

# Rollback last migration
.\scripts\migrate.ps1 -Action remove -Context main  # Windows
./scripts/migrate.sh remove "" main                # Linux/macOS
```

## 🔧 Troubleshooting

### Connection Issues
Scripts automatically load connection strings from `.env.docker`. If you encounter connection errors:
- Verify `.env.docker` exists in project root
- Check connection string format: `Host=...;Port=...;Database=...;Username=...;Password=...`

### Migration Already Applied
Remove command uses `--force` flag to remove migrations even if database connection fails.

### Permission Denied (Linux/macOS)
```bash
chmod +x scripts/migrate.sh
```

## 📚 Full Documentation
See [MIGRATIONS.md](MIGRATIONS.md) for detailed documentation.
