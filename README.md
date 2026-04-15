# PRN232 G9 — Auto Grading Tool

Hệ thống chấm điểm tự động (Auto Grading Tool) được xây dựng bằng **ASP.NET Core 8**, theo kiến trúc **Clean Architecture**, phục vụ cho môn học PRN232 — nhóm 9.

---

## Mục lục

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Kiến trúc hệ thống](#2-kiến-trúc-hệ-thống)
3. [Công nghệ sử dụng](#3-công-nghệ-sử-dụng)
4. [Yêu cầu môi trường](#4-yêu-cầu-môi-trường)
5. [Cài đặt biến môi trường](#5-cài-đặt-biến-môi-trường)
6. [Quick Start](#6-quick-start)
7. [Chạy với Docker](#7-chạy-với-docker)
8. [Chạy trực tiếp (Local Dev)](#8-chạy-trực-tiếp-local-dev)
9. [Quản lý Migrations](#9-quản-lý-migrations)
10. [Auto Migration khi khởi động](#10-auto-migration-khi-khởi-động)
11. [API Endpoints](#11-api-endpoints)
12. [Chuẩn response API](#12-chuẩn-response-api)
13. [Tài khoản mặc định](#13-tài-khoản-mặc-định)
14. [Best Practices](#14-best-practices)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Tổng quan dự án

**Auto Grading Tool** là hệ thống backend API cho phép:

- Quản lý người dùng, vai trò (phân quyền)
- Hỗ trợ chấm điểm tự động
- Gửi email thông báo kết quả
- Xử lý background jobs (Hangfire) cho các tác vụ bất đồng bộ
- Lưu trữ file (local hoặc AWS S3)
- Localization đa ngôn ngữ (Tiếng Anh, Tiếng Việt)

API được document đầy đủ qua **Swagger UI** tại `http://localhost:5000/swagger` (Development).

---

## 2. Kiến trúc hệ thống

Dự án triển khai **Clean Architecture** với 4 layer:

```
src/
├── PRN232_G9_AutoGradingTool.API/            # Presentation Layer
│   ├── Controllers/                          # HTTP endpoints
│   ├── Middlewares/                          # Global exception, JWT, Validation
│   ├── Configurations/                       # Service & pipeline setup
│   └── Extensions/                          # Migration, Seeding bootstrap
│
├── PRN232_G9_AutoGradingTool.Application/    # Application Layer (Use Cases)
│   ├── Features/                            # CQRS Commands & Queries
│   ├── Common/                              # DTOs, Validators, Interfaces, Mappings
│   └── Resources/                           # Localization .resx files
│
├── PRN232_G9_AutoGradingTool.Infrastructure/ # Infrastructure Layer
│   ├── Context/                             # EF Core DbContext (Main + Outer)
│   ├── Migrations/                          # EF Core migration files
│   ├── Repositories/                        # Generic Repository + UoW
│   ├── Services/                            # Identity, JWT, Email, Firebase, File...
│   └── Configurations/                      # Hangfire, EF config
│
└── PRN232_G9_AutoGradingTool.Domain/         # Domain Layer
    ├── Entities/                            # AppUser, AppRole
    ├── Enums/                               # Domain enumerations
    └── Common/                              # Base entity, interfaces
```

### Hai database độc lập

| Context | Database | Nội dung |
|---------|----------|----------|
| `PRN232_G9_AutoGradingToolDbContext` | `autogradingtool_db` | Users, Roles, Identity, Business data |
| `PRN232_G9_AutoGradingToolOuterDbContext` | `autogradingtool_outerdb` | Hangfire jobs, System logs, Background data |

---

## 3. Công nghệ sử dụng

| Thành phần | Công nghệ / Thư viện |
|------------|----------------------|
| Runtime | .NET 8 / ASP.NET Core 8 |
| Database | PostgreSQL 15 |
| Cache | Redis 7 |
| ORM | Entity Framework Core 8 + Npgsql |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| CQRS | MediatR 12 |
| Mapping | AutoMapper 12 |
| Validation | FluentValidation 12 |
| Background Jobs | Hangfire 1.8 (PostgreSQL storage) |
| Logging | Serilog |
| API Docs | Swagger / Swashbuckle |
| File Storage | Local Storage / AWS S3 |
| Email | SMTP (Gmail support) |
| Storage service | Firebase (optional) |
| Container | Docker + Docker Compose |
| DB Admin | pgAdmin 4 |

---

## 4. Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (nếu chạy Docker)
- [PostgreSQL 15+](https://www.postgresql.org/) (nếu chạy local, không dùng Docker)

---

## 5. Cài đặt biến môi trường

### Bước 1: Tạo file `.env`

```bash
cp .env.example .env
```

### Bước 2: Chỉnh sửa `.env` theo môi trường của bạn

```env
# =============================================
# PostgreSQL
# =============================================
POSTGRES_USER=admin
POSTGRES_PASSWORD=admin@123
POSTGRES_DB=postgres_db
POSTGRES_MULTIPLE_DATABASES=autogradingtool_db,autogradingtool_outerdb
DB_HOST=postgres          # Docker: "postgres" | Local: "localhost"
DB_PORT=5432

# =============================================
# Connection Strings
# =============================================
# Docker
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=autogradingtool_db;Username=admin;Password=admin@123;
ConnectionStrings__OuterDbConnection=Host=postgres;Port=5432;Database=autogradingtool_outerdb;Username=admin;Password=admin@123;

# Local (thay postgres → localhost)
# ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=autogradingtool_db;Username=admin;Password=admin@123;
# ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=autogradingtool_outerdb;Username=admin;Password=admin@123;

# =============================================
# JWT (bắt buộc đổi key trong production)
# =============================================
Jwt__Key=REPLACE_WITH_STRONG_JWT_KEY_AT_LEAST_32_CHARS
Jwt__Issuer=Autogradingtool
Jwt__Audience=AutogradingtoolClients
Jwt__ExpiresInMinutes=60
Jwt__RefreshTokenExpiresInDays=7

# =============================================
# Admin account mặc định (dùng khi seed)
# =============================================
AdminUser__Email=admin@autogradingtool.com
AdminUser__DefaultPassword=Admin@123

# =============================================
# Redis
# =============================================
REDIS_PASSWORD=redispassword
REDIS__Configuration=redis:6379,password=redispassword

# =============================================
# Email (SMTP)
# =============================================
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SenderEmail=your-email@gmail.com
EmailSettings__SenderPassword=YOUR_EMAIL_APP_PASSWORD
EmailSettings__EnableSsl=true
EmailSettings__SenderName=No Reply - AutoGradingTool

# =============================================
# File Storage (LocalStorage hoặc S3)
# =============================================
FileStorage__ProviderType=LocalStorage
FileStorage__MaxFileSizeBytes=52428800
FileStorage__Local__RootPath=uploads
# FileStorage__ProviderType=S3
# FileStorage__S3__BucketName=YOUR_BUCKET
# FileStorage__S3__Region=ap-southeast-1
# FileStorage__S3__AccessKey=YOUR_ACCESS_KEY
# FileStorage__S3__SecretKey=YOUR_SECRET_KEY

# =============================================
# Hangfire
# =============================================
Hangfire__UseOuterDatabase=true
Hangfire__WorkerCount=2
Hangfire__Queues=notification-system

# =============================================
# CORS
# =============================================
Cors__AllowedOrigins=http://localhost:3000,http://localhost:5173,http://localhost:4200

# =============================================
# Localization
# =============================================
Localization__SupportedCultures=en,vi
Localization__DefaultCulture=en

# =============================================
# Feature Flags
# =============================================
DataSeeding__EnableSeeding=true

# =============================================
# pgAdmin (development only)
# =============================================
PGADMIN_DEFAULT_EMAIL=admin@autogradingtool.com
PGADMIN_DEFAULT_PASSWORD=Admin@123
```

### Priority thứ tự load env file

File sau sẽ **override** file trước:

1. `.env` — Base (commit lên git)
2. `.env.local` — Local overrides (gitignored)
3. `.env.{ASPNETCORE_ENVIRONMENT}` — Environment-specific
4. `.env.docker` — Docker overrides

---

## 6. Quick Start

Cách nhanh nhất để chạy toàn bộ dự án:

```bash
# 1. Clone project
git clone <repository-url>
cd PRN232_G9_AutoGradingTool

# 2. Tạo file .env
cp .env.example .env
# Chỉnh sửa .env nếu cần (mặc định đã chạy được với Docker)

# 3. Build image và khởi động toàn bộ stack
docker compose up --build -d

# 4. Kiểm tra API
# Swagger: http://localhost:5000/swagger
# Health:  http://localhost:5000/health
```

> `--build` bắt buộc phải có ở lần đầu chạy và mỗi khi thay đổi code — không có flag này Docker sẽ không build image mới.

> Khi container API khởi động ở môi trường `Development`, migrations và seed dữ liệu được **tự động apply** — không cần thao tác thêm.

---

## 7. Chạy với Docker

> Toàn bộ infrastructure (PostgreSQL, Redis, pgAdmin) và API đều được container hóa.

### Lần đầu chạy / Sau khi thay đổi code

```bash
# Đảm bảo .env đã được tạo và DB_HOST=postgres
# --build là bắt buộc để build Docker image từ source code
docker compose up --build -d
```

### Chạy lại (không có thay đổi code)

```bash
# Chỉ dùng khi image đã được build trước đó và code không thay đổi
docker compose up -d
```

### Kiểm tra logs

```bash
docker compose logs -f autogradingtool-api
```

### Dừng và xóa containers

```bash
docker compose down
```

### Xóa cả dữ liệu (volumes)

```bash
docker compose down -v
```

### Các service và port mặc định

| Service | URL / Port |
|---------|------------|
| API | `http://localhost:5000` |
| Swagger UI | `http://localhost:5000/swagger` |
| Hangfire Dashboard | `http://localhost:5000/hangfire` |
| pgAdmin | `http://localhost:5050` |
| PostgreSQL | `localhost:5432` |
| Redis | `localhost:6379` |

> Port có thể override qua biến `POSTGRES_PORT`, `REDIS_PORT`, `PGADMIN_PORT`, `API_PORT` trong `.env`.

---

## 8. Chạy trực tiếp (Local Dev)

> Yêu cầu: PostgreSQL và Redis đã chạy sẵn (hoặc chỉ chạy infra qua Docker).

### Chỉ chạy infrastructure (không chạy API bằng Docker)

```bash
docker compose up postgres redis pgadmin -d
```

### Cập nhật `.env` cho local

```env
DB_HOST=localhost
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=autogradingtool_db;Username=admin;Password=admin@123;
ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=autogradingtool_outerdb;Username=admin;Password=admin@123;
REDIS__Configuration=localhost:6379,password=redispassword
```

### Chạy API

```bash
# Restore dependencies (lần đầu hoặc sau khi thêm package)
dotnet restore

cd src/PRN232_G9_AutoGradingTool.API

# Chạy thông thường
dotnet run

# Chạy với hot reload (khuyến nghị khi dev)
dotnet watch run
```

> **Khi chạy ở môi trường Development**, ứng dụng sẽ **tự động apply migrations** và **seed dữ liệu** khi khởi động.

---

## 9. Quản lý Migrations

Dự án có script tiện ích hỗ trợ quản lý EF Core migrations cho cả 2 database contexts.

### Windows (PowerShell)

```powershell
# Xem danh sách migrations
.\scripts\migrations\migrate.ps1 -Action list

# Tạo migration mới (main DB)
.\scripts\migrations\migrate.ps1 -Action add -Name "AddNewTable" -Context main

# Tạo migration mới (outer DB — Hangfire/Logs)
.\scripts\migrations\migrate.ps1 -Action add -Name "AddAuditLog" -Context outer

# Tạo migration cho cả 2 DB
.\scripts\migrations\migrate.ps1 -Action add -Name "SomeMigration" -Context both

# Apply migrations
.\scripts\migrations\migrate.ps1 -Action update -Context main
.\scripts\migrations\migrate.ps1 -Action update -Context outer

# Rollback migration cuối
.\scripts\migrations\migrate.ps1 -Action remove -Context main

# Dùng env file cụ thể
.\scripts\migrations\migrate.ps1 -Action list -EnvFile ".env.local"
```

### Linux / macOS (Bash)

```bash
# Cấp quyền thực thi (lần đầu)
chmod +x scripts/migrations/migrate.sh

# Xem danh sách migrations
./scripts/migrations/migrate.sh list

# Tạo migration mới (main DB)
./scripts/migrations/migrate.sh add "AddNewTable" main

# Tạo migration mới (outer DB)
./scripts/migrations/migrate.sh add "AddAuditLog" outer

# Apply migrations
./scripts/migrations/migrate.sh update "" main
./scripts/migrations/migrate.sh update "" outer

# Rollback migration cuối
./scripts/migrations/migrate.sh remove "" main

# Dùng env file cụ thể
./scripts/migrations/migrate.sh list "" both ".env.local"
```

### Context options

| Context | Database | Nội dung |
|---------|----------|----------|
| `main` | `autogradingtool_db` | Users, Roles, Identity, Business data |
| `outer` | `autogradingtool_outerdb` | Hangfire, System logs, Background jobs |
| `both` | Cả hai | Mặc định nếu không chỉ định |

### Câu lệnh EF Core thủ công

```powershell
# Tạo migration thủ công (PowerShell — load .env trước)
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), 'Process')
    }
}

# Main DB
dotnet ef migrations add YourMigrationName `
  --context PRN232_G9_AutoGradingToolDbContext `
  --output-dir Migrations/PRN232_G9_AutoGradingTool `
  --project src/PRN232_G9_AutoGradingTool.Infrastructure

# Outer DB
dotnet ef migrations add YourMigrationName `
  --context PRN232_G9_AutoGradingToolOuterDbContext `
  --output-dir Migrations/PRN232_G9_AutoGradingToolOuter `
  --project src/PRN232_G9_AutoGradingTool.Infrastructure
```

---

## 10. Auto Migration khi khởi động

Khi `ASPNETCORE_ENVIRONMENT=Development`, ứng dụng tự động thực hiện theo thứ tự:

1. **Apply main DB migrations** — `PRN232_G9_AutoGradingToolDbContext`
2. **Apply outer DB migrations** — `PRN232_G9_AutoGradingToolOuterDbContext`
3. **Khởi tạo Hangfire storage** (dùng OuterDb)
4. **Đăng ký Hangfire recurring jobs**
5. **Seed dữ liệu ban đầu** (admin account, roles, ...)

> Trong môi trường **Production / Staging**, auto migration bị tắt. Bạn cần chạy migrations thủ công trước khi deploy.

---

## 11. API Endpoints

### Authentication (`/api/cms/auth`)

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `POST` | `/api/cms/auth/login` | ❌ | Đăng nhập, nhận access token + refresh token |
| `POST` | `/api/cms/auth/logout` | ✅ Bearer | Đăng xuất, xóa refresh token |
| `POST` | `/api/cms/auth/refresh-token` | ❌ | Làm mới access token bằng refresh token |
| `GET`  | `/api/cms/auth/profile` | ✅ Bearer | Lấy thông tin người dùng hiện tại |
| `POST` | `/api/cms/auth/change-password` | ✅ Bearer | Đổi mật khẩu |

> Xem đầy đủ tại **Swagger UI**: `http://localhost:5000/swagger`  
> OpenAPI JSON: `http://localhost:5000/swagger/v1/swagger.json`

---

## 12. Chuẩn response API

Toàn bộ API trả về cấu trúc `Result<T>` thống nhất:

**Thành công:**
```json
{
  "isSuccess": true,
  "message": "Login successfully!",
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "abc...",
    "expiresIn": 3600
  }
}
```

**Thất bại:**
```json
{
  "isSuccess": false,
  "message": "Email hoặc mật khẩu không đúng",
  "errors": ["Invalid credentials"],
  "errorCode": "Unauthorized"
}
```

**Lỗi validation:**
```json
{
  "isSuccess": false,
  "message": "Dữ liệu không hợp lệ",
  "errors": [
    "Email không được để trống",
    "Mật khẩu phải có ít nhất 8 ký tự"
  ],
  "errorCode": "ValidationFailed"
}
```

> Ngôn ngữ của message phụ thuộc vào header `Accept-Language: vi` hoặc `Accept-Language: en`.

---

## 13. Tài khoản mặc định

Khi `DataSeeding__EnableSeeding=true`, hệ thống tự seed tài khoản admin:

| Thông tin | Giá trị |
|-----------|---------|
| Email | `admin@autogradingtool.com` |
| Password | `Admin@123` |

> Thay đổi qua biến `AdminUser__Email` và `AdminUser__DefaultPassword` trong `.env`.

---

## 14. Best Practices

### Cho Developers

- Dùng **localization** cho tất cả message hiển thị ra người dùng (`.resx` files)
- Theo **CQRS pattern** — mỗi feature có Commands và Queries riêng biệt
- Thêm **CancellationToken** vào tất cả async methods
- Dùng **`AsNoTracking()`** cho các query chỉ đọc
- Dùng **migration scripts** (`migrate.ps1` / `migrate.sh`) thay vì chạy `dotnet ef` thủ công
- Document API bằng XML comments trên controller actions

### Cho DevOps

- Dùng **Docker Compose** để đảm bảo môi trường nhất quán giữa các thành viên
- **Không commit** file `.env` lên Git — chỉ commit `.env.example`
- Volume `logs_data` và `uploads` đã được mount — **không mất dữ liệu khi rebuild**
- Xem logs từ volume: `docker compose logs -f autogradingtool-api`
- Copy logs ra host để backup: `docker cp autogradingtool-api:/app/Logs ./logs-backup`
- Trong **Production**: tắt auto migration, chạy migrations thủ công trước khi deploy

---

## 15. Troubleshooting

### Lỗi kết nối database (`No such host is known`)

Nguyên nhân: Connection string trỏ đến `Host=postgres` (Docker hostname) trong khi đang chạy local.

**Giải pháp**: Tạo `.env.local` với connection string dùng `localhost`:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=autogradingtool_db;Username=admin;Password=admin@123;
ConnectionStrings__OuterDbConnection=Host=localhost;Port=5432;Database=autogradingtool_outerdb;Username=admin;Password=admin@123;
```

### Lỗi migrations khi database chưa sẵn sàng

Container API có thể start trước PostgreSQL. Docker Compose đã cấu hình `healthcheck` và `depends_on` để xử lý vấn đề này.  
Ngoài ra, `MigrationExtension` có retry logic (3 lần, cách nhau 5 giây).

### `Permission denied` trên Linux/macOS

```bash
chmod +x scripts/migrations/migrate.sh
```

### Xem logs chi tiết

```bash
# Docker
docker compose logs -f autogradingtool-api

# Local
# Log được ghi bởi Serilog theo cấu hình trong appsettings.json
```
