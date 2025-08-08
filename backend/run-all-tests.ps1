# Real Estate API - Complete Test Suite Runner
# This script runs all types of tests: Unit, Integration, and Performance

Write-Host "🧪 Starting Complete Test Suite for Real Estate API" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# Function to check if a command was successful
function Test-Success {
    param($ExitCode, $TestType)
    if ($ExitCode -eq 0) {
        Write-Host "✅ $TestType tests PASSED" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ $TestType tests FAILED" -ForegroundColor Red
        return $false
    }
}

$AllTestsPassed = $true

# 1. Unit Tests
Write-Host "`n🔬 Running Unit Tests..." -ForegroundColor Yellow
dotnet test RealEstate.Tests/RealEstate.Tests.csproj --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
$UnitTestsResult = $LASTEXITCODE
$AllTestsPassed = $AllTestsPassed -and (Test-Success $UnitTestsResult "Unit")

# 2. Integration Tests
Write-Host "`n🔗 Running Integration Tests..." -ForegroundColor Yellow
dotnet test RealEstate.Tests.Integration/RealEstate.Tests.Integration.csproj --configuration Release --logger "console;verbosity=normal"
$IntegrationTestsResult = $LASTEXITCODE
$AllTestsPassed = $AllTestsPassed -and (Test-Success $IntegrationTestsResult "Integration")

# 3. Performance Benchmarks
Write-Host "`n⚡ Running Performance Benchmarks..." -ForegroundColor Yellow
Write-Host "Note: This may take several minutes..." -ForegroundColor Cyan

# Build performance tests in Release mode for accurate benchmarks
dotnet build RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release --no-restore

# Run benchmarks
dotnet run --project RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release -- --filter "*PropertyServiceBenchmarks*"
$BenchmarkResult = $LASTEXITCODE
$AllTestsPassed = $AllTestsPassed -and (Test-Success $BenchmarkResult "Performance Benchmark")

# 4. Load Tests
Write-Host "`n🔥 Running Load Tests..." -ForegroundColor Yellow
Write-Host "Note: This will test API under simulated load..." -ForegroundColor Cyan

dotnet test RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release --logger "console;verbosity=normal" --filter "Category=LoadTest"
$LoadTestResult = $LASTEXITCODE
$AllTestsPassed = $AllTestsPassed -and (Test-Success $LoadTestResult "Load")

# 5. Frontend Tests (if available)
if (Test-Path "../frontend/package.json") {
    Write-Host "`n⚛️ Running Frontend Tests..." -ForegroundColor Yellow
    Push-Location "../frontend"
    
    # Check if test script exists
    $packageJson = Get-Content "package.json" | ConvertFrom-Json
    if ($packageJson.scripts.test) {
        npm test -- --watchAll=false --coverage
        $FrontendTestResult = $LASTEXITCODE
        $AllTestsPassed = $AllTestsPassed -and (Test-Success $FrontendTestResult "Frontend")
    } else {
        Write-Host "⚠️ No frontend test script found" -ForegroundColor Yellow
    }
    
    Pop-Location
}

# Summary
Write-Host "`n📊 TEST SUITE SUMMARY" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan

if ($AllTestsPassed) {
    Write-Host "🎉 ALL TESTS PASSED! 🎉" -ForegroundColor Green
    Write-Host "Your application is ready for production!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "💥 SOME TESTS FAILED!" -ForegroundColor Red
    Write-Host "Please review the test results above." -ForegroundColor Red
    exit 1
}
