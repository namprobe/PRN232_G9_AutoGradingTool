# Các lệnh tạo dự án PRN232_G9_AutoGradingTool - Clean Architecture

Copy và paste các lệnh sau vào terminal để tạo dự án:

## PowerShell (Windows)

```powershell
# Tạo cấu trúc thư mục
New-Item -ItemType Directory -Force -Path "PRN232_G9_AutoGradingTool\src" | Out-Null
cd PRN232_G9_AutoGradingTool

# Tạo Solution
dotnet new sln -n PRN232_G9_AutoGradingTool

# Tạo Domain Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Domain -f net8.0 -o src\PRN232_G9_AutoGradingTool.Domain
dotnet sln add src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

# Tạo Application Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Application -f net8.0 -o src\PRN232_G9_AutoGradingTool.Application
dotnet sln add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj

# Tạo Infrastructure Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Infrastructure -f net8.0 -o src\PRN232_G9_AutoGradingTool.Infrastructure
dotnet sln add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj

# Tạo API Layer
dotnet new webapi -n PRN232_G9_AutoGradingTool.API -f net8.0 -o src\PRN232_G9_AutoGradingTool.API
dotnet sln add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj

# Thiết lập dependencies
dotnet add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

# Xóa file mặc định
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs" -ErrorAction SilentlyContinue
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Application\Class1.cs" -ErrorAction SilentlyContinue
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs" -ErrorAction SilentlyContinue

# Restore packages
dotnet restore
```

## Bash/Linux/Mac

```bash
# Tạo cấu trúc thư mục
mkdir -p PRN232_G9_AutoGradingTool/src
cd PRN232_G9_AutoGradingTool

# Tạo Solution
dotnet new sln -n PRN232_G9_AutoGradingTool

# Tạo Domain Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Domain -f net8.0 -o src/PRN232_G9_AutoGradingTool.Domain
dotnet sln add src/PRN232_G9_AutoGradingTool.Domain/PRN232_G9_AutoGradingTool.Domain.csproj

# Tạo Application Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Application -f net8.0 -o src/PRN232_G9_AutoGradingTool.Application
dotnet sln add src/PRN232_G9_AutoGradingTool.Application/PRN232_G9_AutoGradingTool.Application.csproj

# Tạo Infrastructure Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Infrastructure -f net8.0 -o src/PRN232_G9_AutoGradingTool.Infrastructure
dotnet sln add src/PRN232_G9_AutoGradingTool.Infrastructure/PRN232_G9_AutoGradingTool.Infrastructure.csproj

# Tạo API Layer
dotnet new webapi -n PRN232_G9_AutoGradingTool.API -f net8.0 -o src/PRN232_G9_AutoGradingTool.API
dotnet sln add src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj

# Thiết lập dependencies
dotnet add src/PRN232_G9_AutoGradingTool.Application/PRN232_G9_AutoGradingTool.Application.csproj reference src/PRN232_G9_AutoGradingTool.Domain/PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src/PRN232_G9_AutoGradingTool.Infrastructure/PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src/PRN232_G9_AutoGradingTool.Application/PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src/PRN232_G9_AutoGradingTool.Infrastructure/PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src/PRN232_G9_AutoGradingTool.Domain/PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj reference src/PRN232_G9_AutoGradingTool.Application/PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj reference src/PRN232_G9_AutoGradingTool.Infrastructure/PRN232_G9_AutoGradingTool.Infrastructure.csproj
dotnet add src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj reference src/PRN232_G9_AutoGradingTool.Domain/PRN232_G9_AutoGradingTool.Domain.csproj

# Xóa file mặc định
rm -f src/PRN232_G9_AutoGradingTool.Domain/Class1.cs
rm -f src/PRN232_G9_AutoGradingTool.Application/Class1.cs
rm -f src/PRN232_G9_AutoGradingTool.Infrastructure/Class1.cs

# Restore packages
dotnet restore
```

## CMD (Windows Command Prompt)

```cmd
REM Tạo cấu trúc thư mục
if not exist "PRN232_G9_AutoGradingTool\src" mkdir "PRN232_G9_AutoGradingTool\src"
cd PRN232_G9_AutoGradingTool

REM Tạo Solution
dotnet new sln -n PRN232_G9_AutoGradingTool

REM Tạo Domain Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Domain -f net8.0 -o src\PRN232_G9_AutoGradingTool.Domain
dotnet sln add src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

REM Tạo Application Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Application -f net8.0 -o src\PRN232_G9_AutoGradingTool.Application
dotnet sln add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj

REM Tạo Infrastructure Layer
dotnet new classlib -n PRN232_G9_AutoGradingTool.Infrastructure -f net8.0 -o src\PRN232_G9_AutoGradingTool.Infrastructure
dotnet sln add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj

REM Tạo API Layer
dotnet new webapi -n PRN232_G9_AutoGradingTool.API -f net8.0 -o src\PRN232_G9_AutoGradingTool.API
dotnet sln add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj

REM Thiết lập dependencies
dotnet add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

REM Xóa file mặc định
if exist "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs"
if exist "src\PRN232_G9_AutoGradingTool.Application\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Application\Class1.cs"
if exist "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs"

REM Restore packages
dotnet restore
```

## Sau khi tạo xong

```bash
# Build solution
dotnet build

# Chạy API
dotnet run --project src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj
```


