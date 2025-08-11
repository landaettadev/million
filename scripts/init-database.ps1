#!/usr/bin/env pwsh

Write-Host "🗄️ MongoDB Database Initialization - Million Real Estate" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

param(
    [string]$DatabaseName = "million",
    [string]$MongoUri = "mongodb://localhost:27017",
    [switch]$Force
)

# Check if MongoDB is accessible
Write-Host "🔍 Checking MongoDB connection..." -ForegroundColor Yellow

try {
    $pingTest = mongosh --eval "db.runCommand({ping: 1})" --quiet 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error: Cannot connect to MongoDB at $MongoUri" -ForegroundColor Red
        Write-Host "   Please ensure MongoDB is running and accessible" -ForegroundColor White
        exit 1
    }
    
    Write-Host "✅ MongoDB connection successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: MongoDB connection failed" -ForegroundColor Red
    exit 1
}

# Check if database already exists and has data
if (-not $Force) {
    Write-Host "`n🔍 Checking existing database..." -ForegroundColor Yellow
    
    $checkScript = @"
use $DatabaseName
db.stats()
"@
    
    $dbStats = $checkScript | mongosh --quiet 2>$null
    
    if ($dbStats -and $dbStats -notmatch "database does not exist") {
        $collections = @("owners", "properties", "propertyImages", "propertyTraces")
        $hasData = $false
        
        foreach ($collection in $collections) {
            $countScript = @"
use $DatabaseName
db.$collection.countDocuments()
"@
            $count = $countScript | mongosh --quiet 2>$null
            if ($count -match "^\d+" -and [int]$count -gt 0) {
                $hasData = $true
                break
            }
        }
        
        if ($hasData) {
            Write-Host "⚠️  Warning: Database '$DatabaseName' already exists with data!" -ForegroundColor Yellow
            $response = Read-Host "Do you want to continue? This will overwrite existing data. (y/N)"
            if ($response -ne "y" -and $response -ne "Y") {
                Write-Host "❌ Initialization cancelled by user" -ForegroundColor Red
                exit 0
            }
        }
    }
}

Write-Host "`n🚀 Starting database initialization..." -ForegroundColor Cyan

try {
    # Create database and collections
    Write-Host "📁 Creating database and collections..." -ForegroundColor Yellow
    
    $initScript = @"
use $DatabaseName

// Create collections with validation
db.createCollection("owners", {
    validator: {
        \$jsonSchema: {
            bsonType: "object",
            required: ["name", "address"],
            properties: {
                name: { bsonType: "string" },
                address: { bsonType: "string" },
                photo: { bsonType: "string" },
                birthday: { bsonType: "date" }
            }
        }
    }
})

db.createCollection("properties", {
    validator: {
        \$jsonSchema: {
            bsonType: "object",
            required: ["name", "address", "price", "operationType"],
            properties: {
                name: { bsonType: "string" },
                address: { bsonType: "string" },
                price: { bsonType: "number" },
                codeInternal: { bsonType: "string" },
                year: { bsonType: "int" },
                operationType: { enum: ["sale", "rent"] },
                description: { bsonType: "string" },
                beds: { bsonType: "int" },
                baths: { bsonType: "int" },
                halfBaths: { bsonType: "int" },
                sqft: { bsonType: "int" },
                ownerId: { bsonType: "objectId" }
            }
        }
    }
})

db.createCollection("propertyImages", {
    validator: {
        \$jsonSchema: {
            bsonType: "object",
            required: ["propertyId", "file", "enabled"],
            properties: {
                propertyId: { bsonType: "objectId" },
                file: { bsonType: "string" },
                enabled: { bsonType: "bool" }
            }
        }
    }
})

db.createCollection("propertyTraces", {
    validator: {
        \$jsonSchema: {
            bsonType: "object",
            required: ["propertyId", "dateSale", "name", "value", "tax"],
            properties: {
                propertyId: { bsonType: "objectId" },
                dateSale: { bsonType: "date" },
                name: { bsonType: "string" },
                value: { bsonType: "number" },
                tax: { bsonType: "number" }
            }
        }
    }
})

// Create indexes for better performance
db.properties.createIndex({ "price": 1 })
db.properties.createIndex({ "name": "text", "address": "text" })
db.properties.createIndex({ "operationType": 1 })
db.properties.createIndex({ "ownerId": 1 })

db.propertyImages.createIndex({ "propertyId": 1 })
db.propertyImages.createIndex({ "enabled": 1 })

db.propertyTraces.createIndex({ "propertyId": 1 })
db.propertyTraces.createIndex({ "dateSale": -1 })

db.owners.createIndex({ "name": "text" })

print("Database and collections created successfully")
"@
    
    $initScript | mongosh --quiet > $null 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database structure created successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating database structure" -ForegroundColor Red
        throw "Database structure creation failed"
    }
    
    # Insert sample data
    Write-Host "`n📊 Inserting sample data..." -ForegroundColor Yellow
    
    # Sample owners
    $ownersData = @(
        @{
            name = "María González"
            address = "Calle Mayor 123, Madrid"
            photo = "maria-gonzalez.jpg"
            birthday = "1985-03-15T00:00:00Z"
        },
        @{
            name = "Carlos Rodríguez"
            address = "Avenida de la Paz 456, Barcelona"
            photo = "carlos-rodriguez.jpg"
            birthday = "1978-07-22T00:00:00Z"
        },
        @{
            name = "Ana Martínez"
            address = "Plaza España 789, Valencia"
            photo = "ana-martinez.jpg"
            birthday = "1990-11-08T00:00:00Z"
        },
        @{
            name = "Luis Fernández"
            address = "Gran Vía 321, Sevilla"
            photo = "luis-fernandez.jpg"
            birthday = "1982-04-12T00:00:00Z"
        }
    )
    
    Write-Host "   👥 Inserting owners..." -ForegroundColor White
    foreach ($owner in $ownersData) {
        $ownerJson = $owner | ConvertTo-Json -Compress
        $insertScript = @"
use $DatabaseName
db.owners.insertOne($ownerJson)
"@
        $insertScript | mongosh --quiet > $null 2>$null
    }
    
    # Get owner IDs for properties
    $ownerIds = @()
    $getOwnersScript = @"
use $DatabaseName
db.owners.find({}, {_id: 1}).forEach(function(owner) {
    print(owner._id)
})
"@
    
    $ownerIds = $getOwnersScript | mongosh --quiet 2>$null | Where-Object { $_ -match "^[a-f0-9]{24}$" }
    
    if ($ownerIds.Count -eq 0) {
        Write-Host "❌ Error: No owners found after insertion" -ForegroundColor Red
        exit 1
    }
    
    # Sample properties
    $propertiesData = @(
        @{
            name = "Luxury Penthouse Madrid"
            address = "Paseo de la Castellana 123, Madrid"
            price = 850000
            codeInternal = "MAD001"
            year = 2020
            operationType = "sale"
            description = "Exclusive penthouse with panoramic views of Madrid"
            beds = 4
            baths = 3
            halfBaths = 1
            sqft = 3500
            ownerId = $ownerIds[0]
        },
        @{
            name = "Modern Apartment Barcelona"
            address = "Diagonal 456, Barcelona"
            price = 450000
            codeInternal = "BCN001"
            year = 2019
            operationType = "sale"
            description = "Contemporary apartment in the heart of Barcelona"
            beds = 3
            baths = 2
            halfBaths = 0
            sqft = 2200
            ownerId = $ownerIds[1]
        },
        @{
            name = "Beach House Valencia"
            address = "Malvarrosa Beach 789, Valencia"
            price = 650000
            codeInternal = "VAL001"
            year = 2021
            operationType = "sale"
            description = "Beautiful beachfront property with private access"
            beds = 5
            baths = 4
            halfBaths = 1
            sqft = 4200
            ownerId = $ownerIds[2]
        },
        @{
            name = "Downtown Loft Sevilla"
            address = "Calle Sierpes 321, Sevilla"
            price = 280000
            codeInternal = "SEV001"
            year = 2018
            operationType = "sale"
            description = "Charming loft in the historic center of Sevilla"
            beds = 2
            baths = 1
            halfBaths = 0
            sqft = 1500
            ownerId = $ownerIds[3]
        },
        @{
            name = "Garden Villa Madrid"
            address = "Calle Serrano 654, Madrid"
            price = 1200000
            codeInternal = "MAD002"
            year = 2022
            operationType = "sale"
            description = "Spacious villa with private garden and pool"
            beds = 6
            baths = 5
            halfBaths = 2
            sqft = 5500
            ownerId = $ownerIds[0]
        },
        @{
            name = "Rental Studio Barcelona"
            address = "Gracia 987, Barcelona"
            price = 1200
            codeInternal = "BCN002"
            year = 2020
            operationType = "rent"
            description = "Cozy studio perfect for young professionals"
            beds = 1
            baths = 1
            halfBaths = 0
            sqft = 800
            ownerId = $ownerIds[1]
        }
    )
    
    Write-Host "   🏠 Inserting properties..." -ForegroundColor White
    foreach ($property in $propertiesData) {
        $propertyJson = $property | ConvertTo-Json -Compress
        $insertScript = @"
use $DatabaseName
db.properties.insertOne($propertyJson)
"@
        $insertScript | mongosh --quiet > $null 2>$null
    }
    
    # Get property IDs for images
    $propertyIds = @()
    $getPropertiesScript = @"
use $DatabaseName
db.properties.find({}, {_id: 1, codeInternal: 1}).forEach(function(property) {
    print(JSON.stringify(property))
})
"@
    
    $propertyResults = $getPropertiesScript | mongosh --quiet 2>$null | Where-Object { $_ -match "^\{" }
    
    foreach ($result in $propertyResults) {
        try {
            $property = $result | ConvertFrom-Json
            $propertyIds += @{
                id = $property._id
                code = $property.codeInternal
            }
        } catch {
            # Skip invalid results
        }
    }
    
    # Sample property images
    Write-Host "   🖼️  Inserting property images..." -ForegroundColor White
    
    foreach ($property in $propertyIds) {
        $images = @()
        
        switch ($property.code) {
            "MAD001" { $images = @("madrid-penthouse-1.jpg", "madrid-penthouse-2.jpg", "madrid-penthouse-3.jpg") }
            "BCN001" { $images = @("barcelona-apartment-1.jpg", "barcelona-apartment-2.jpg") }
            "VAL001" { $images = @("valencia-beach-1.jpg", "valencia-beach-2.jpg", "valencia-beach-3.jpg") }
            "SEV001" { $images = @("sevilla-loft-1.jpg", "sevilla-loft-2.jpg") }
            default { $images = @("default-property-1.jpg") }
        }
        
        foreach ($image in $images) {
            $imageData = @{
                propertyId = $property.id
                file = $image
                enabled = $true
            }
            
            $imageJson = $imageData | ConvertTo-Json -Compress
            $insertScript = @"
use $DatabaseName
db.propertyImages.insertOne($imageJson)
"@
            $insertScript | mongosh --quiet > $null 2>$null
        }
    }
    
    # Sample property traces
    Write-Host "   📈 Inserting property traces..." -ForegroundColor White
    
    $tracesData = @(
        @{
            propertyId = $propertyIds[0].id
            dateSale = "2023-01-15T00:00:00Z"
            name = "Previous Sale 1"
            value = 800000
            tax = 48000
        },
        @{
            propertyId = $propertyIds[1].id
            dateSale = "2022-06-20T00:00:00Z"
            name = "Previous Sale 2"
            value = 420000
            tax = 25200
        }
    )
    
    foreach ($trace in $tracesData) {
        $traceJson = $trace | ConvertTo-Json -Compress
        $insertScript = @"
use $DatabaseName
db.propertyTraces.insertOne($traceJson)
"@
        $insertScript | mongosh --quiet > $null 2>$null
    }
    
    # Verify data insertion
    Write-Host "`n🔍 Verifying data insertion..." -ForegroundColor Yellow
    
    $verifyScript = @"
use $DatabaseName
print("Database Statistics:")
print("==================")
print("Owners: " + db.owners.countDocuments())
print("Properties: " + db.properties.countDocuments())
print("Property Images: " + db.propertyImages.countDocuments())
print("Property Traces: " + db.propertyTraces.countDocuments())
"@
    
    $stats = $verifyScript | mongosh --quiet 2>$null
    
    Write-Host "`n📊 Database Statistics:" -ForegroundColor Yellow
    $stats | ForEach-Object {
        if ($_ -match ":" -and $_ -notmatch "Database Statistics") {
            Write-Host "   $_" -ForegroundColor White
        }
    }
    
    Write-Host "`n✅ Database initialization completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Database initialization failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Database is ready to use!" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Start the backend API" -ForegroundColor White
Write-Host "   2. Start the frontend application" -ForegroundColor White
Write-Host "   3. Test the application functionality" -ForegroundColor White
Write-Host "   4. Run tests to ensure everything works" -ForegroundColor White

Write-Host "`n🚀 Ready to go!" -ForegroundColor Green
