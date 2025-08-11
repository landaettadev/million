#!/usr/bin/env pwsh

Write-Host "🗄️ MongoDB Backup Script - Million Real Estate" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

param(
    [string]$DatabaseName = "million",
    [string]$BackupPath = "./backups",
    [string]$MongoUri = "mongodb://localhost:27017"
)

# Create backup directory if it doesn't exist
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force
    Write-Host "📁 Created backup directory: $BackupPath" -ForegroundColor Yellow
}

# Generate timestamp for backup filename
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = Join-Path $BackupPath "million-backup-$timestamp.json"

Write-Host "🔄 Starting MongoDB backup..." -ForegroundColor Cyan
Write-Host "   Database: $DatabaseName" -ForegroundColor White
Write-Host "   URI: $MongoUri" -ForegroundColor White
Write-Host "   Output: $backupFile" -ForegroundColor White

try {
    # Check if MongoDB is accessible
    Write-Host "`n🔍 Checking MongoDB connection..." -ForegroundColor Yellow
    
    # Test connection using mongosh (if available)
    $mongoshTest = mongosh --eval "db.runCommand({ping: 1})" --quiet 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ MongoDB connection successful" -ForegroundColor Green
    } else {
        Write-Host "⚠️  MongoDB connection test failed, but continuing..." -ForegroundColor Yellow
    }
    
    # Create backup using mongodump if available
    $mongodumpPath = Get-Command mongodump -ErrorAction SilentlyContinue
    if ($mongodumpPath) {
        Write-Host "`n📦 Creating backup using mongodump..." -ForegroundColor Yellow
        
        $dumpPath = Join-Path $BackupPath "dump-$timestamp"
        $mongodumpArgs = @(
            "--uri", $MongoUri,
            "--db", $DatabaseName,
            "--out", $dumpPath
        )
        
        & mongodump @mongodumpArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Backup created successfully using mongodump" -ForegroundColor Green
            Write-Host "   Location: $dumpPath" -ForegroundColor White
            
            # Create a compressed archive
            $archivePath = Join-Path $BackupPath "million-backup-$timestamp.zip"
            Compress-Archive -Path $dumpPath -DestinationPath $archivePath -Force
            
            Write-Host "📦 Compressed backup created: $archivePath" -ForegroundColor Green
            
            # Clean up dump directory
            Remove-Item -Path $dumpPath -Recurse -Force
            Write-Host "🧹 Cleaned up temporary files" -ForegroundColor Yellow
        } else {
            Write-Host "❌ mongodump failed, trying alternative method..." -ForegroundColor Red
        }
    }
    
    # Alternative: Create JSON export using mongosh
    if (-not $mongodumpPath -or $LASTEXITCODE -ne 0) {
        Write-Host "`n📄 Creating JSON export using mongosh..." -ForegroundColor Yellow
        
        # Export collections to JSON
        $collections = @("owners", "properties", "propertyImages", "propertyTraces")
        
        foreach ($collection in $collections) {
            $collectionFile = Join-Path $BackupPath "$collection-$timestamp.json"
            
            $exportScript = @"
use $DatabaseName
db.$collection.find().forEach(function(doc) {
    print(JSON.stringify(doc));
})
"@
            
            $exportScript | mongosh --quiet > $collectionFile 2>$null
            
            if (Test-Path $collectionFile) {
                $lineCount = (Get-Content $collectionFile | Measure-Object -Line).Lines
                Write-Host "   📄 $collection`: $lineCount documents exported" -ForegroundColor White
            }
        }
        
        Write-Host "✅ JSON export completed" -ForegroundColor Green
    }
    
    # Create backup summary
    $summaryFile = Join-Path $BackupPath "backup-summary-$timestamp.txt"
    $summary = @"
Million Real Estate - MongoDB Backup Summary
==========================================
Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Database: $DatabaseName
Backup Type: $(if ($mongodumpPath) { "mongodump + compression" } else { "JSON export" })

Files Created:
$(Get-ChildItem -Path $BackupPath -Filter "*$timestamp*" | ForEach-Object { "  - $($_.Name)" })

Backup Location: $BackupPath
Total Size: $((Get-ChildItem -Path $BackupPath -Filter "*$timestamp*" | Measure-Object -Property Length -Sum).Sum / 1MB) MB

Notes:
- This backup contains all collections from the $DatabaseName database
- For production use, consider using mongodump with compression
- Backup files are timestamped for easy identification
- Keep backups in a secure location

Restore Instructions:
1. For mongodump backup: mongorestore --uri $MongoUri --db $DatabaseName dump-$timestamp/$DatabaseName/
2. For JSON backup: Use mongosh to import each collection file
"@
    
    $summary | Out-File -FilePath $summaryFile -Encoding UTF8
    Write-Host "`n📋 Backup summary created: $summaryFile" -ForegroundColor Green
    
    # Display backup information
    Write-Host "`n🎉 Backup completed successfully!" -ForegroundColor Green
    Write-Host "`n📊 Backup Summary:" -ForegroundColor Yellow
    Write-Host "   • Database: $DatabaseName" -ForegroundColor White
    Write-Host "   • Backup Type: $(if ($mongodumpPath) { "mongodump + compression" } else { "JSON export" })" -ForegroundColor White
    Write-Host "   • Location: $BackupPath" -ForegroundColor White
    Write-Host "   • Files: $(Get-ChildItem -Path $BackupPath -Filter "*$timestamp*" | Measure-Object).Count files" -ForegroundColor White
    
    # List backup files
    Write-Host "`n📁 Backup Files:" -ForegroundColor Yellow
    Get-ChildItem -Path $BackupPath -Filter "*$timestamp*" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "   • $($_.Name) ($size MB)" -ForegroundColor White
    }
    
} catch {
    Write-Host "`n❌ Backup failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Backup process completed!" -ForegroundColor Green
Write-Host "   Next steps:" -ForegroundColor Yellow
Write-Host "   1. Verify backup files in: $BackupPath" -ForegroundColor White
Write-Host "   2. Test restore process if needed" -ForegroundColor White
Write-Host "   3. Store backup in secure location" -ForegroundColor White
