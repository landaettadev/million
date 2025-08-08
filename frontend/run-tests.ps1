# Frontend Test Runner Script
# This script runs all frontend tests with coverage and generates reports

Write-Host "🧪 Starting Frontend Test Suite" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Check if we're in the frontend directory
if (-not (Test-Path "package.json")) {
    Write-Host "❌ Error: package.json not found. Please run this script from the frontend directory." -ForegroundColor Red
    exit 1
}

# Install dependencies if node_modules doesn't exist
if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
        exit 1
    }
}

# Run unit tests
Write-Host "`n🔬 Running Unit Tests..." -ForegroundColor Yellow
npm test -- --coverage --watchAll=false --passWithNoTests
$UnitTestResult = $LASTEXITCODE

# Run tests with coverage report
Write-Host "`n📊 Generating Coverage Report..." -ForegroundColor Yellow
npm run test:coverage
$CoverageResult = $LASTEXITCODE

# Run tests in watch mode for development (optional)
if ($args -contains "--watch") {
    Write-Host "`n👀 Starting tests in watch mode..." -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop watching" -ForegroundColor Gray
    npm run test:watch
}

# Summary
Write-Host "`n📋 Test Summary" -ForegroundColor Green
Write-Host "===============" -ForegroundColor Green

if ($UnitTestResult -eq 0) {
    Write-Host "✅ Unit tests PASSED" -ForegroundColor Green
} else {
    Write-Host "❌ Unit tests FAILED" -ForegroundColor Red
}

if ($CoverageResult -eq 0) {
    Write-Host "✅ Coverage report generated" -ForegroundColor Green
} else {
    Write-Host "❌ Coverage report failed" -ForegroundColor Red
}

# Check if all tests passed
$AllTestsPassed = ($UnitTestResult -eq 0) -and ($CoverageResult -eq 0)

if ($AllTestsPassed) {
    Write-Host "`n🎉 All frontend tests completed successfully!" -ForegroundColor Green
    Write-Host "📁 Coverage report available in: coverage/lcov-report/index.html" -ForegroundColor Cyan
} else {
    Write-Host "`n💥 Some tests failed. Please review the output above." -ForegroundColor Red
    exit 1
}

Write-Host "`n✨ Frontend testing complete!" -ForegroundColor Green
