# Script tạo dự án PRN232_G9_AutoGradingTool theo Clean Architecture
# Target Framework: .NET 6

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tạo dự án PRN232_G9_AutoGradingTool - Clean Architecture" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Tạo thư mục gốc và thư mục src
Write-Host "1. Tạo cấu trúc thư mục..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "PRN232_G9_AutoGradingTool\src" | Out-Null
Set-Location "PRN232_G9_AutoGradingTool"

# Tạo Solution
Write-Host "2. Tạo Solution PRN232_G9_AutoGradingTool.sln..." -ForegroundColor Yellow
dotnet new sln -n PRN232_G9_AutoGradingTool

# Tạo các project theo Clean Architecture
Write-Host "3. Tạo Domain Layer (Class Library)..." -ForegroundColor Yellow
dotnet new classlib -n PRN232_G9_AutoGradingTool.Domain -f net8.0 -o src\PRN232_G9_AutoGradingTool.Domain
dotnet sln add src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

Write-Host "4. Tạo Application Layer (Class Library)..." -ForegroundColor Yellow
dotnet new classlib -n PRN232_G9_AutoGradingTool.Application -f net8.0 -o src\PRN232_G9_AutoGradingTool.Application
dotnet sln add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj

Write-Host "5. Tạo Infrastructure Layer (Class Library)..." -ForegroundColor Yellow
dotnet new classlib -n PRN232_G9_AutoGradingTool.Infrastructure -f net8.0 -o src\PRN232_G9_AutoGradingTool.Infrastructure
dotnet sln add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj

Write-Host "6. Tạo API Layer (Web API)..." -ForegroundColor Yellow
dotnet new webapi -n PRN232_G9_AutoGradingTool.API -f net8.0 -o src\PRN232_G9_AutoGradingTool.API
dotnet sln add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj

# Thiết lập dependencies giữa các layers
Write-Host "7. Thiết lập dependencies giữa các layers..." -ForegroundColor Yellow

# Application phụ thuộc vào Domain
dotnet add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

# Infrastructure phụ thuộc vào Application và Domain
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

# API phụ thuộc vào tất cả các layers
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

# Xóa các file mặc định không cần thiết
Write-Host "8. Dọn dẹp các file mặc định..." -ForegroundColor Yellow
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs" -ErrorAction SilentlyContinue
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Application\Class1.cs" -ErrorAction SilentlyContinue
Remove-Item -Path "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs" -ErrorAction SilentlyContinue

# Restore packages
Write-Host "9. Restore NuGet packages..." -ForegroundColor Yellow
dotnet restore

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Hoàn thành! Dự án đã được tạo thành công." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Cấu trúc dự án:" -ForegroundColor Cyan
Write-Host "PRN232_G9_AutoGradingTool/" -ForegroundColor White
Write-Host "├── src/" -ForegroundColor White
Write-Host "│   ├── PRN232_G9_AutoGradingTool.Domain/" -ForegroundColor White
Write-Host "│   ├── PRN232_G9_AutoGradingTool.Application/" -ForegroundColor White
Write-Host "│   ├── PRN232_G9_AutoGradingTool.Infrastructure/" -ForegroundColor White
Write-Host "│   └── PRN232_G9_AutoGradingTool.API/" -ForegroundColor White
Write-Host "└── PRN232_G9_AutoGradingTool.sln" -ForegroundColor White
Write-Host ""
Write-Host "Để build dự án, chạy lệnh: dotnet build" -ForegroundColor Yellow
Write-Host "Để chạy API, chạy lệnh: dotnet run --project src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj" -ForegroundColor Yellow


