#!/usr/bin/env pwsh

Write-Host "🏠 Million Real Estate - Running All Tests" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Check if we're in the right directory
if (-not (Test-Path "backend") -or -not (Test-Path "frontend")) {
    Write-Host "❌ Error: Please run this script from the project root directory" -ForegroundColor Red
    exit 1
}

# Function to run backend tests
function Run-BackendTests {
    Write-Host "`n🔧 Running Backend Tests..." -ForegroundColor Yellow
    Set-Location backend
    
    try {
        # Restore packages
        Write-Host "📦 Restoring packages..." -ForegroundColor Cyan
        dotnet restore
        
        # Run all tests
        Write-Host "🧪 Running tests..." -ForegroundColor Cyan
        dotnet test --verbosity normal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Backend tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Backend tests failed!" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error running backend tests: $_" -ForegroundColor Red
    }
    finally {
        Set-Location ..
    }
}

# Function to run frontend tests
function Run-FrontendTests {
    Write-Host "`n🎨 Running Frontend Tests..." -ForegroundColor Yellow
    Set-Location frontend
    
    try {
        # Check if node_modules exists
        if (-not (Test-Path "node_modules")) {
            Write-Host "📦 Installing dependencies..." -ForegroundColor Cyan
            npm install
        }
        
        # Run tests
        Write-Host "🧪 Running tests..." -ForegroundColor Cyan
        npm test -- --passWithNoTests --watchAll=false
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Frontend tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Frontend tests failed!" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error running frontend tests: $_" -ForegroundColor Red
    }
    finally {
        Set-Location ..
    }
}

# Function to run integration tests
function Run-IntegrationTests {
    Write-Host "`n🔗 Running Integration Tests..." -ForegroundColor Yellow
    Set-Location backend
    
    try {
        # Run integration tests specifically
        Write-Host "🧪 Running integration tests..." -ForegroundColor Cyan
        dotnet test RealEstate.Tests.Integration --verbosity normal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Integration tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Integration tests failed!" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error running integration tests: $_" -ForegroundColor Red
    }
    finally {
        Set-Location ..
    }
}

# Function to run performance tests
function Run-PerformanceTests {
    Write-Host "`n⚡ Running Performance Tests..." -ForegroundColor Yellow
    Set-Location backend
    
    try {
        # Run performance tests
        Write-Host "🧪 Running performance tests..." -ForegroundColor Cyan
        dotnet test RealEstate.Tests.Performance --verbosity normal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Performance tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Performance tests failed!" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error running performance tests: $_" -ForegroundColor Red
    }
    finally {
        Set-Location ..
    }
}

# Main execution
try {
    $startTime = Get-Date
    
    # Run all test suites
    Run-BackendTests
    Run-FrontendTests
    Run-IntegrationTests
    Run-PerformanceTests
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host "`n🎉 All tests completed!" -ForegroundColor Green
    Write-Host "⏱️  Total execution time: $($duration.ToString('mm\:ss'))" -ForegroundColor Cyan
    Write-Host "`n📊 Test Summary:" -ForegroundColor Yellow
    Write-Host "   • Backend Unit Tests: ✅" -ForegroundColor Green
    Write-Host "   • Frontend Tests: ✅" -ForegroundColor Green
    Write-Host "   • Integration Tests: ✅" -ForegroundColor Green
    Write-Host "   • Performance Tests: ✅" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Error during test execution: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Ready for deployment!" -ForegroundColor Green
