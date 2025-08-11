#!/usr/bin/env pwsh

Write-Host "🔄 MongoDB Restore Script - Million Real Estate" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupPath,
    [string]$DatabaseName = "million",
    [string]$MongoUri = "mongodb://localhost:27017",
    [switch]$Force
)

# Validate backup path
if (-not (Test-Path $BackupPath)) {
    Write-Host "❌ Error: Backup path '$BackupPath' does not exist" -ForegroundColor Red
    exit 1
}

Write-Host "🔄 Starting MongoDB restore..." -ForegroundColor Cyan
Write-Host "   Database: $DatabaseName" -ForegroundColor White
Write-Host "   URI: $MongoUri" -ForegroundColor White
Write-Host "   Backup: $BackupPath" -ForegroundColor White

# Check if database already exists and has data
if (-not $Force) {
    Write-Host "`n🔍 Checking existing database..." -ForegroundColor Yellow
    
    $checkScript = @"
use $DatabaseName
db.stats()
"@
    
    $dbStats = $checkScript | mongosh --quiet 2>$null
    
    if ($dbStats -and $dbStats -notmatch "database does not exist") {
        $collectionCount = ($checkScript | mongosh --quiet | Select-String "collections").Count
        if ($collectionCount -gt 0) {
            Write-Host "⚠️  Warning: Database '$DatabaseName' already exists with data!" -ForegroundColor Yellow
            $response = Read-Host "Do you want to continue? This will overwrite existing data. (y/N)"
            if ($response -ne "y" -and $response -ne "Y") {
                Write-Host "❌ Restore cancelled by user" -ForegroundColor Red
                exit 0
            }
        }
    }
}

try {
    # Check MongoDB connection
    Write-Host "`n🔍 Testing MongoDB connection..." -ForegroundColor Yellow
    $pingTest = mongosh --eval "db.runCommand({ping: 1})" --quiet 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error: Cannot connect to MongoDB at $MongoUri" -ForegroundColor Red
        Write-Host "   Please ensure MongoDB is running and accessible" -ForegroundColor White
        exit 1
    }
    
    Write-Host "✅ MongoDB connection successful" -ForegroundColor Green
    
    # Determine backup type and restore method
    if (Test-Path (Join-Path $BackupPath "*.zip")) {
        # Compressed mongodump backup
        $zipFile = Get-ChildItem -Path $BackupPath -Filter "*.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        Write-Host "`n📦 Detected compressed backup: $($zipFile.Name)" -ForegroundColor Yellow
        
        # Extract backup
        $extractPath = Join-Path $BackupPath "temp-restore"
        if (Test-Path $extractPath) {
            Remove-Item -Path $extractPath -Recurse -Force
        }
        
        Write-Host "📁 Extracting backup..." -ForegroundColor Cyan
        Expand-Archive -Path $zipFile.FullName -DestinationPath $extractPath
        
        # Find the database folder
        $dbFolder = Get-ChildItem -Path $extractPath -Directory | Where-Object { $_.Name -eq $DatabaseName }
        if ($dbFolder) {
            $restorePath = $dbFolder.FullName
        } else {
            $restorePath = $extractPath
        }
        
        Write-Host "🔄 Restoring using mongorestore..." -ForegroundColor Yellow
        
        $restoreArgs = @(
            "--uri", $MongoUri,
            "--db", $DatabaseName,
            "--drop",  # Drop existing collections
            $restorePath
        )
        
        & mongorestore @restoreArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Restore completed successfully using mongorestore" -ForegroundColor Green
        } else {
            Write-Host "❌ mongorestore failed" -ForegroundColor Red
            throw "mongorestore failed with exit code $LASTEXITCODE"
        }
        
        # Clean up
        Remove-Item -Path $extractPath -Recurse -Force
        
    } elseif (Test-Path (Join-Path $BackupPath "dump-*")) {
        # Uncompressed mongodump backup
        $dumpFolder = Get-ChildItem -Path $BackupPath -Directory -Filter "dump-*" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        Write-Host "`n📁 Detected mongodump backup: $($dumpFolder.Name)" -ForegroundColor Yellow
        
        $restorePath = Join-Path $dumpFolder.FullName $DatabaseName
        
        if (Test-Path $restorePath) {
            Write-Host "🔄 Restoring using mongorestore..." -ForegroundColor Yellow
            
            $restoreArgs = @(
                "--uri", $MongoUri,
                "--db", $DatabaseName,
                "--drop",
                $restorePath
            )
            
            & mongorestore @restoreArgs
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Restore completed successfully using mongorestore" -ForegroundColor Green
            } else {
                Write-Host "❌ mongorestore failed" -ForegroundColor Red
                throw "mongorestore failed with exit code $LASTEXITCODE"
            }
        } else {
            Write-Host "❌ Error: Database folder not found in backup" -ForegroundColor Red
            exit 1
        }
        
    } elseif (Test-Path (Join-Path $BackupPath "*.json")) {
        # JSON export backup
        Write-Host "`n📄 Detected JSON export backup" -ForegroundColor Yellow
        
        # Get all JSON files
        $jsonFiles = Get-ChildItem -Path $BackupPath -Filter "*.json" | Where-Object { $_.Name -notmatch "backup-summary" }
        
        if ($jsonFiles.Count -eq 0) {
            Write-Host "❌ Error: No JSON backup files found" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "🔄 Restoring collections from JSON files..." -ForegroundColor Yellow
        
        foreach ($jsonFile in $jsonFiles) {
            $collectionName = $jsonFile.Name -replace "-\d{8}-\d{6}\.json$", ""
            Write-Host "   📄 Restoring collection: $collectionName" -ForegroundColor White
            
            # Drop existing collection
            $dropScript = @"
use $DatabaseName
db.$collectionName.drop()
"@
            $dropScript | mongosh --quiet > $null 2>$null
            
            # Import JSON data
            $importScript = @"
use $DatabaseName
const data = JSON.parse(cat('$($jsonFile.FullName)'))
if (Array.isArray(data)) {
    db.$collectionName.insertMany(data)
} else {
    db.$collectionName.insertOne(data)
}
"@
            
            $importScript | mongosh --quiet > $null 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "     ✅ $collectionName restored successfully" -ForegroundColor Green
            } else {
                Write-Host "     ❌ $collectionName restore failed" -ForegroundColor Red
            }
        }
        
    } else {
        Write-Host "❌ Error: No valid backup files found in $BackupPath" -ForegroundColor Red
        Write-Host "   Supported formats:" -ForegroundColor White
        Write-Host "     • .zip files (compressed mongodump)" -ForegroundColor White
        Write-Host "     • dump-* folders (mongodump)" -ForegroundColor White
        Write-Host "     • .json files (JSON export)" -ForegroundColor White
        exit 1
    }
    
    # Verify restore
    Write-Host "`n🔍 Verifying restore..." -ForegroundColor Yellow
    
    $verifyScript = @"
use $DatabaseName
db.stats()
"@
    
    $stats = $verifyScript | mongosh --quiet 2>$null
    
    if ($stats -and $stats -notmatch "database does not exist") {
        $collections = @("owners", "properties", "propertyImages", "propertyTraces")
        
        Write-Host "`n📊 Database Statistics:" -ForegroundColor Yellow
        foreach ($collection in $collections) {
            $countScript = @"
use $DatabaseName
db.$collection.countDocuments()
"@
            $count = $countScript | mongosh --quiet 2>$null
            if ($count -match "^\d+$") {
                Write-Host "   • $collection`: $count documents" -ForegroundColor White
            } else {
                Write-Host "   • $collection`: 0 documents" -ForegroundColor White
            }
        }
        
        Write-Host "`n✅ Restore verification completed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Error: Database verification failed" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "`n❌ Restore failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 MongoDB restore completed successfully!" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Verify data integrity" -ForegroundColor White
Write-Host "   2. Test application functionality" -ForegroundColor White
Write-Host "   3. Update application configuration if needed" -ForegroundColor White
Write-Host "   4. Run tests to ensure everything works" -ForegroundColor White

Write-Host "`n🚀 Ready to use!" -ForegroundColor Green
