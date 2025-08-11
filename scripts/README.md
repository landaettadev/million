# 🗄️ MongoDB Scripts - Million Real Estate

This directory contains PowerShell scripts for managing the MongoDB database used by the Million Real Estate application.

## 📁 Available Scripts

### 1. `init-database.ps1` - Database Initialization
Creates the database structure and populates it with sample data.

**Usage:**
```powershell
# Initialize database with default settings
.\init-database.ps1

# Initialize with custom database name
.\init-database.ps1 -DatabaseName "million_prod"

# Force initialization (overwrite existing data)
.\init-database.ps1 -Force
```

**What it does:**
- Creates database and collections with proper validation schemas
- Creates performance indexes for optimal query performance
- Inserts sample data:
  - 4 sample owners
  - 6 sample properties (mix of sale and rent)
  - Property images for each property
  - Sample property traces (sales history)

### 2. `mongodb-backup.ps1` - Database Backup
Creates a complete backup of the MongoDB database.

**Usage:**
```powershell
# Create backup with default settings
.\mongodb-backup.ps1

# Create backup with custom settings
.\mongodb-backup.ps1 -DatabaseName "million" -BackupPath "./my-backups" -MongoUri "mongodb://localhost:27017"
```

**What it does:**
- Attempts to use `mongodump` for binary backup (recommended)
- Falls back to JSON export if `mongodump` is not available
- Creates compressed archives for easy storage
- Generates backup summary with statistics
- Supports multiple backup formats

### 3. `mongodb-restore.ps1` - Database Restore
Restores the database from a backup.

**Usage:**
```powershell
# Restore from backup directory
.\mongodb-restore.ps1 -BackupPath "./backups"

# Restore with custom settings
.\mongodb-restore.ps1 -BackupPath "./backups" -DatabaseName "million" -MongoUri "mongodb://localhost:27017"

# Force restore (overwrite existing data)
.\mongodb-restore.ps1 -BackupPath "./backups" -Force
```

**What it does:**
- Automatically detects backup format (zip, dump folder, or JSON)
- Restores data using appropriate method (`mongorestore` or JSON import)
- Verifies restore success
- Provides detailed statistics after restore

## 🚀 Quick Start

### 1. First Time Setup
```powershell
# Navigate to scripts directory
cd scripts

# Initialize database with sample data
.\init-database.ps1
```

### 2. Regular Backup
```powershell
# Create daily backup
.\mongodb-backup.ps1

# Backup will be saved to ./backups/ directory
```

### 3. Restore from Backup
```powershell
# Restore from latest backup
.\mongodb-restore.ps1 -BackupPath "./backups"
```

## 📋 Prerequisites

### Required Tools
- **PowerShell 5.1+** or **PowerShell Core 6.0+**
- **MongoDB** running locally or accessible via network
- **mongosh** (MongoDB Shell) - for connection testing
- **mongodump/mongorestore** (optional, for binary backups)

### MongoDB Connection
- Default: `mongodb://localhost:27017`
- Ensure MongoDB is running and accessible
- Check connection with: `mongosh --eval "db.runCommand({ping: 1})"`

## 🔧 Configuration

### Environment Variables
You can set these environment variables to customize behavior:

```powershell
# Set custom MongoDB URI
$env:MONGODB_URI = "mongodb://username:password@host:port/database"

# Set custom backup path
$env:BACKUP_PATH = "C:\my-backups"

# Enable verbose logging
$env:VERBOSE = "true"
```

### Custom Parameters
All scripts support command-line parameters for customization:

```powershell
# Database name
-DatabaseName "million_prod"

# MongoDB connection URI
-MongoUri "mongodb://localhost:27017"

# Backup/restore path
-BackupPath "./custom-backups"

# Force operations (overwrite existing data)
-Force
```

## 📊 Sample Data Structure

### Owners Collection
```json
{
  "name": "María González",
  "address": "Calle Mayor 123, Madrid",
  "photo": "maria-gonzalez.jpg",
  "birthday": "1985-03-15T00:00:00Z"
}
```

### Properties Collection
```json
{
  "name": "Luxury Penthouse Madrid",
  "address": "Paseo de la Castellana 123, Madrid",
  "price": 850000,
  "codeInternal": "MAD001",
  "year": 2020,
  "operationType": "sale",
  "description": "Exclusive penthouse with panoramic views",
  "beds": 4,
  "baths": 3,
  "halfBaths": 1,
  "sqft": 3500,
  "ownerId": "ObjectId(...)"
}
```

### Property Images Collection
```json
{
  "propertyId": "ObjectId(...)",
  "file": "madrid-penthouse-1.jpg",
  "enabled": true
}
```

### Property Traces Collection
```json
{
  "propertyId": "ObjectId(...)",
  "dateSale": "2023-01-15T00:00:00Z",
  "name": "Previous Sale 1",
  "value": 800000,
  "tax": 48000
}
```

## 🗂️ Backup File Formats

### 1. Compressed Backup (.zip)
- Created by `mongodump` + compression
- Most efficient and complete
- Best for production use
- Contains binary data and indexes

### 2. Uncompressed Dump
- Created by `mongodump`
- Larger size but faster creation
- Good for local development

### 3. JSON Export
- Human-readable format
- Easy to inspect and modify
- Good for data migration
- Contains only data (no indexes)

## 🔒 Security Considerations

### Development Environment
- Scripts use default MongoDB connection (no authentication)
- Backup files contain sensitive data
- Store backups in secure location

### Production Environment
- Use authenticated MongoDB connections
- Encrypt backup files
- Implement backup rotation
- Monitor backup success/failure

## 🚨 Troubleshooting

### Common Issues

#### 1. MongoDB Connection Failed
```powershell
# Check if MongoDB is running
mongosh --eval "db.runCommand({ping: 1})"

# Verify connection string
# Check firewall settings
# Ensure MongoDB service is started
```

#### 2. Permission Denied
```powershell
# Run PowerShell as Administrator
# Check file permissions
# Verify MongoDB user permissions
```

#### 3. Backup/Restore Failed
```powershell
# Check available disk space
# Verify MongoDB version compatibility
# Check log files for detailed errors
```

### Debug Mode
Enable verbose logging for troubleshooting:

```powershell
# Set environment variable
$env:VERBOSE = "true"

# Run script with debug information
.\mongodb-backup.ps1 -Verbose
```

## 📈 Performance Tips

### Backup Optimization
- Use `mongodump` for large databases
- Schedule backups during low-traffic periods
- Compress backups to save storage space
- Use incremental backups when possible

### Restore Optimization
- Restore to SSD storage for better performance
- Close other applications during restore
- Use `mongorestore` with `--numParallelCollections` for parallel processing

## 🔄 Automation

### Scheduled Backups
Create a Windows Task Scheduler job:

```powershell
# Create scheduled task
SCHTASKS /CREATE /SC DAILY /TN "MongoDB Backup" /TR "powershell.exe -File C:\path\to\mongodb-backup.ps1" /ST 02:00
```

### PowerShell Profile
Add to your PowerShell profile for quick access:

```powershell
# Add to $PROFILE
function Backup-MillionDB { & "C:\path\to\scripts\mongodb-backup.ps1" }
function Restore-MillionDB { & "C:\path\to\scripts\mongodb-restore.ps1" -BackupPath "./backups" }
```

## 📞 Support

If you encounter issues:

1. Check this documentation
2. Review script error messages
3. Verify MongoDB connection
4. Check PowerShell execution policy
5. Contact the development team

---

**Happy database management! 🗄️✨**
