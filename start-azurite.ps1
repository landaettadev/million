# Start Azurite for Azure Storage development
# This script starts Azurite in the background for local development

Write-Host "Starting Azurite for Azure Storage development..." -ForegroundColor Green

# Check if Azurite is installed
try {
    azurite --version | Out-Null
    Write-Host "Azurite is installed. Starting..." -ForegroundColor Green
} catch {
    Write-Host "Azurite is not installed. Please install it first:" -ForegroundColor Yellow
    Write-Host "npm install -g azurite" -ForegroundColor Cyan
    Write-Host "Or use Docker: docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite" -ForegroundColor Cyan
    exit 1
}

# Start Azurite in the background
Start-Process -FilePath "azurite" -ArgumentList "--silent" -WindowStyle Hidden

Write-Host "Azurite started in the background." -ForegroundColor Green
Write-Host "Azure Storage endpoints:" -ForegroundColor Cyan
Write-Host "  - Blob: http://127.0.0.1:10000" -ForegroundColor White
Write-Host "  - Queue: http://127.0.0.1:10001" -ForegroundColor White
Write-Host "  - Table: http://127.0.0.1:10002" -ForegroundColor White
Write-Host ""
Write-Host "To stop Azurite, run: Get-Process azurite | Stop-Process" -ForegroundColor Yellow
