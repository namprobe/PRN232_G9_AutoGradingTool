@echo off
REM Script tạo dự án PRN232_G9_AutoGradingTool theo Clean Architecture
REM Target Framework: .NET 6

echo ========================================
echo Tạo dự án PRN232_G9_AutoGradingTool - Clean Architecture
echo ========================================
echo.

REM Tạo thư mục gốc và thư mục src
echo 1. Tạo cấu trúc thư mục...
if not exist "PRN232_G9_AutoGradingTool\src" mkdir "PRN232_G9_AutoGradingTool\src"
cd PRN232_G9_AutoGradingTool

REM Tạo Solution
echo 2. Tạo Solution PRN232_G9_AutoGradingTool.sln...
dotnet new sln -n PRN232_G9_AutoGradingTool

REM Tạo các project theo Clean Architecture
echo 3. Tạo Domain Layer (Class Library)...
dotnet new classlib -n PRN232_G9_AutoGradingTool.Domain -f net8.0 -o src\PRN232_G9_AutoGradingTool.Domain
dotnet sln add src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

echo 4. Tạo Application Layer (Class Library)...
dotnet new classlib -n PRN232_G9_AutoGradingTool.Application -f net8.0 -o src\PRN232_G9_AutoGradingTool.Application
dotnet sln add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj

echo 5. Tạo Infrastructure Layer (Class Library)...
dotnet new classlib -n PRN232_G9_AutoGradingTool.Infrastructure -f net8.0 -o src\PRN232_G9_AutoGradingTool.Infrastructure
dotnet sln add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj

echo 6. Tạo API Layer (Web API)...
dotnet new webapi -n PRN232_G9_AutoGradingTool.API -f net8.0 -o src\PRN232_G9_AutoGradingTool.API
dotnet sln add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj

REM Thiết lập dependencies giữa các layers
echo 7. Thiết lập dependencies giữa các layers...

REM Application phụ thuộc vào Domain
dotnet add src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

REM Infrastructure phụ thuộc vào Application và Domain
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

REM API phụ thuộc vào tất cả các layers
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Application\PRN232_G9_AutoGradingTool.Application.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Infrastructure\PRN232_G9_AutoGradingTool.Infrastructure.csproj
dotnet add src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj reference src\PRN232_G9_AutoGradingTool.Domain\PRN232_G9_AutoGradingTool.Domain.csproj

REM Xóa các file mặc định không cần thiết
echo 8. Dọn dẹp các file mặc định...
if exist "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Domain\Class1.cs"
if exist "src\PRN232_G9_AutoGradingTool.Application\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Application\Class1.cs"
if exist "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs" del "src\PRN232_G9_AutoGradingTool.Infrastructure\Class1.cs"

REM Restore packages
echo 9. Restore NuGet packages...
dotnet restore

echo.
echo ========================================
echo Hoàn thành! Dự án đã được tạo thành công.
echo ========================================
echo.
echo Cấu trúc dự án:
echo PRN232_G9_AutoGradingTool/
echo ├── src/
echo │   ├── PRN232_G9_AutoGradingTool.Domain/
echo │   ├── PRN232_G9_AutoGradingTool.Application/
echo │   ├── PRN232_G9_AutoGradingTool.Infrastructure/
echo │   └── PRN232_G9_AutoGradingTool.API/
echo └── PRN232_G9_AutoGradingTool.sln
echo.
echo Để build dự án, chạy lệnh: dotnet build
echo Để chạy API, chạy lệnh: dotnet run --project src\PRN232_G9_AutoGradingTool.API\PRN232_G9_AutoGradingTool.API.csproj
pause


