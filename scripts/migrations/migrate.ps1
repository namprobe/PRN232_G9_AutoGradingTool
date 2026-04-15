param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("add", "update", "remove", "list")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$Name,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("both", "main", "outer")]
    [string]$Context = "both",
    
    [Parameter(Mandatory=$false)]
    [string]$EnvFile
)

Write-Host "=== PRN232_G9_AutoGradingTool Migration Tool ===" -ForegroundColor Magenta
Write-Host ""

# Get root directory
$scriptPath = Split-Path -Parent $PSScriptRoot
$rootPath = Split-Path -Parent $scriptPath

# Function to load environment file
function Load-EnvFile {
    param([string]$FilePath)
    
    if (Test-Path $FilePath) {
        Write-Host "Loading: $(Split-Path -Leaf $FilePath)" -ForegroundColor Yellow
        Get-Content $FilePath | ForEach-Object {
            if ($_ -match '^([^#][^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim().Trim('"').Trim("'")
                [Environment]::SetEnvironmentVariable($key, $value, 'Process')
            }
        }
        return $true
    }
    return $false
}

# Load environment files in priority order
Write-Host "Loading environment variables..." -ForegroundColor Cyan
$loaded = $false

if ($EnvFile) {
    # Use user-specified env file
    $customEnvPath = Join-Path $rootPath $EnvFile
    if (Load-EnvFile $customEnvPath) {
        $loaded = $true
    } else {
        Write-Host "[WARN] Specified env file not found: $EnvFile" -ForegroundColor Yellow
    }
} else {
    # Load in priority order (later files override earlier ones)
    $envFiles = @(
        ".env",
        ".env.local",
        ".env.$($env:ASPNETCORE_ENVIRONMENT)",
        ".env.docker"
    )
    
    foreach ($envFileName in $envFiles) {
        $envPath = Join-Path $rootPath $envFileName
        if (Load-EnvFile $envPath) {
            $loaded = $true
        }
    }
}

if ($loaded) {
    Write-Host "[OK] Environment variables loaded" -ForegroundColor Green
} else {
    Write-Host "[WARN] No environment files found" -ForegroundColor Yellow
}
Write-Host ""

# Verify connection strings are loaded
$defaultConn = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
$outerConn = [Environment]::GetEnvironmentVariable("ConnectionStrings__OuterDbConnection")

if ($defaultConn) {
    $displayConn = $defaultConn.Substring(0, [Math]::Min(50, $defaultConn.Length))
    Write-Host "[OK] DefaultConnection: $displayConn..." -ForegroundColor Green
} else {
    Write-Host "[WARN] DefaultConnection not found in environment" -ForegroundColor Yellow
}

if ($outerConn) {
    $displayConn = $outerConn.Substring(0, [Math]::Min(50, $outerConn.Length))
    Write-Host "[OK] OuterDbConnection: $displayConn..." -ForegroundColor Green
} else {
    Write-Host "[WARN] OuterDbConnection not found in environment" -ForegroundColor Yellow
}
Write-Host ""

$infraProject = "src/PRN232_G9_AutoGradingTool.Infrastructure"

# Change to root directory
Push-Location $rootPath

function Run-Migration {
    param(
        [string]$ContextName,
        [string]$OutputDir,
        [string]$Action,
        [string]$Name
    )
    
    Write-Host ">> Processing $ContextName..." -ForegroundColor Cyan
    
    try {
        switch ($Action) {
            "add" {
                if (-not $Name) {
                    Write-Error "Migration name is required for add action"
                    return $false
                }
                dotnet ef migrations add $Name --context $ContextName --output-dir $OutputDir --project $infraProject
            }
            "update" {
                dotnet ef database update --context $ContextName --project $infraProject
            }
            "remove" {
                dotnet ef migrations remove --context $ContextName --project $infraProject --force
            }
            "list" {
                dotnet ef migrations list --context $ContextName --project $infraProject
            }
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] Success" -ForegroundColor Green
            Write-Host ""
            return $true
        } else {
            Write-Host "  [FAIL] Failed with exit code $LASTEXITCODE" -ForegroundColor Red
            Write-Host ""
            return $false
        }
    }
    catch {
        Write-Host "  [FAIL] Error: $_" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Execute based on context
$success = $true

if ($Context -eq "both" -or $Context -eq "main") {
    $result = Run-Migration -ContextName "PRN232_G9_AutoGradingToolDbContext" -OutputDir "Migrations/PRN232_G9_AutoGradingTool" -Action $Action -Name $Name
    $success = $success -and $result
}

if ($Context -eq "both" -or $Context -eq "outer") {
    $result = Run-Migration -ContextName "PRN232_G9_AutoGradingToolOuterDbContext" -OutputDir "Migrations/PRN232_G9_AutoGradingToolOuter" -Action $Action -Name $Name
    $success = $success -and $result
}

Pop-Location

if ($success) {
    Write-Host "=== All operations completed successfully ===" -ForegroundColor Green
} else {
    Write-Host "=== Some operations failed ===" -ForegroundColor Red
    exit 1
}
