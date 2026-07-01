# Build script for GHB DP2 Backend (PowerShell version)
# Usage: .\build.ps1 [options]

param(
    [switch]$Sonar,           # Enable SonarQube analysis (slower build)
    [switch]$Fast,            # Fast build without SonarQube analysis (default)
    [switch]$Clean,           # Clean before build
    [switch]$Test,            # Run tests after build
    [switch]$Coverage,        # Run tests with coverage (implies -Sonar)
    [switch]$Help             # Show help message
)

# Function to show help
function Show-Help {
    Write-Host "Build script for GHB DP2 Backend" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\build.ps1 [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Sonar              Enable SonarQube analysis (slower build)" -ForegroundColor White
    Write-Host "  -Fast               Fast build without SonarQube analysis (default)" -ForegroundColor White
    Write-Host "  -Clean              Clean before build" -ForegroundColor White
    Write-Host "  -Test               Run tests after build" -ForegroundColor White
    Write-Host "  -Coverage           Run tests with coverage (implies -Sonar)" -ForegroundColor White
    Write-Host "  -Help               Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1                    # Fast build (default)" -ForegroundColor Gray
    Write-Host "  .\build.ps1 -Fast -Test        # Fast build with tests" -ForegroundColor Gray
    Write-Host "  .\build.ps1 -Sonar -Test       # Build with SonarQube analysis and tests" -ForegroundColor Gray
    Write-Host "  .\build.ps1 -Coverage          # Build with SonarQube analysis and test coverage" -ForegroundColor Gray
    Write-Host "  .\build.ps1 -Clean -Sonar      # Clean build with SonarQube analysis" -ForegroundColor Gray
}

# Show help if requested
if ($Help) {
    Show-Help
    exit 0
}

# Set default values
$EnableSonar = $false
$BuildConfig = "Release"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendDir = Join-Path (Split-Path -Parent $ScriptDir) "Backend"

# Process parameters
if ($Coverage) {
    $EnableSonar = $true
    $Test = $true
}

if ($Sonar) {
    $EnableSonar = $true
}

if ($Fast) {
    $EnableSonar = $false
}

# If no specific mode is set, default to fast
if (-not $Sonar -and -not $Fast -and -not $Coverage) {
    $EnableSonar = $false
}

# Print build configuration
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GHB DP2 Backend Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Configuration: $BuildConfig" -ForegroundColor White
Write-Host "SonarQube Analysis: $(if ($EnableSonar) { 'Enabled' } else { 'Disabled' })" -ForegroundColor White
Write-Host "Clean Build: $(if ($Clean) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "Run Tests: $(if ($Test) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "Test Coverage: $(if ($Coverage) { 'Yes' } else { 'No' })" -ForegroundColor White
Write-Host "Backend Directory: $BackendDir" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

# Change to backend directory
Set-Location $BackendDir

try {
    # Clean if requested
    if ($Clean) {
        Write-Host "[INFO] Cleaning previous build artifacts..." -ForegroundColor Blue
        dotnet clean GHB.DP2.sln --configuration $BuildConfig
        if ($LASTEXITCODE -ne 0) { throw "Clean failed" }
        Write-Host "[SUCCESS] Clean completed" -ForegroundColor Green
    }

    # Install SonarQube tools if needed
    if ($EnableSonar) {
        Write-Host "[INFO] Installing SonarQube tools..." -ForegroundColor Blue
        dotnet tool install --global dotnet-sonarscanner --ignore-failed-sources
        if ($Coverage) {
            dotnet tool install --global dotnet-coverage --ignore-failed-sources
        }
        Write-Host "[SUCCESS] SonarQube tools installed" -ForegroundColor Green
    }

    # Restore packages
    Write-Host "[INFO] Restoring NuGet packages..." -ForegroundColor Blue
    if ($EnableSonar) {
        dotnet restore GHB.DP2.sln /p:EnableSonarQubeAnalysis=true
    } else {
        dotnet restore GHB.DP2.sln /p:EnableSonarQubeAnalysis=false
    }
    if ($LASTEXITCODE -ne 0) { throw "Package restore failed" }
    Write-Host "[SUCCESS] Package restore completed" -ForegroundColor Green

    # Build
    Write-Host "[INFO] Building solution..." -ForegroundColor Blue
    if ($EnableSonar) {
        Write-Host "[WARNING] Building with SonarQube analysis enabled (this may take longer)..." -ForegroundColor Yellow
        dotnet build GHB.DP2.sln --configuration $BuildConfig --no-restore /p:EnableSonarQubeAnalysis=true
    } else {
        Write-Host "[INFO] Building with fast mode (SonarQube analysis disabled)..." -ForegroundColor Blue
        dotnet build GHB.DP2.sln --configuration $BuildConfig --no-restore /p:EnableSonarQubeAnalysis=false
    }
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "[SUCCESS] Build completed" -ForegroundColor Green

    # Run tests if requested
    if ($Test) {
        Write-Host "[INFO] Running tests..." -ForegroundColor Blue
        if ($Coverage) {
            Write-Host "[INFO] Running tests with coverage..." -ForegroundColor Blue
            dotnet-coverage collect "dotnet test GHB.DP2.sln --no-build --configuration $BuildConfig /p:EnableSonarQubeAnalysis=true" -f xml -o "coverage.xml"
            if ($LASTEXITCODE -ne 0) { throw "Tests with coverage failed" }
            Write-Host "[SUCCESS] Tests with coverage completed" -ForegroundColor Green
        } else {
            if ($EnableSonar) {
                dotnet test GHB.DP2.sln --no-build --configuration $BuildConfig /p:EnableSonarQubeAnalysis=true
            } else {
                dotnet test GHB.DP2.sln --no-build --configuration $BuildConfig /p:EnableSonarQubeAnalysis=false
            }
            if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
            Write-Host "[SUCCESS] Tests completed" -ForegroundColor Green
        }
    }

    # Final message
    Write-Host "========================================" -ForegroundColor Cyan
    if ($EnableSonar) {
        Write-Host "[SUCCESS] Build completed with SonarQube analysis" -ForegroundColor Green
        Write-Host "[INFO] Note: SonarQube analysis was enabled, which may have increased build time" -ForegroundColor Blue
        Write-Host "[INFO] For faster builds, use: .\build.ps1 -Fast" -ForegroundColor Blue
    } else {
        Write-Host "[SUCCESS] Fast build completed" -ForegroundColor Green
        Write-Host "[INFO] SonarQube analysis was disabled for faster build times" -ForegroundColor Blue
        Write-Host "[INFO] For code quality analysis, use: .\build.ps1 -Sonar" -ForegroundColor Blue
    }
    Write-Host "========================================" -ForegroundColor Cyan

} catch {
    Write-Host "[ERROR] Build failed: $_" -ForegroundColor Red
    exit 1
}
