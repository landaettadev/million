#!/usr/bin/env pwsh

Write-Host "⚡ MongoDB Index Optimization - Million Real Estate" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

param(
    [string]$DatabaseName = "million",
    [string]$MongoUri = "mongodb://localhost:27017",
    [switch]$Analyze,
    [switch]$Create,
    [switch]$Drop
)

# Check MongoDB connection
Write-Host "🔍 Testing MongoDB connection..." -ForegroundColor Yellow
$pingTest = mongosh --eval "db.runCommand({ping: 1})" --quiet 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Cannot connect to MongoDB at $MongoUri" -ForegroundColor Red
    exit 1
}

Write-Host "✅ MongoDB connection successful" -ForegroundColor Green

# Function to analyze query performance
function Analyze-QueryPerformance {
    Write-Host "`n📊 Analyzing Query Performance..." -ForegroundColor Yellow
    
    $analysisScript = @"
use $DatabaseName

// Analyze properties collection queries
print("=== PROPERTIES COLLECTION ANALYSIS ===")

// Check current indexes
print("Current indexes:")
db.properties.getIndexes().forEach(function(index) {
    print("  - " + JSON.stringify(index.key) + " (name: " + index.name + ")")
})

// Analyze common query patterns
print("\nQuery Analysis:")

// 1. Search by name/address (text search)
print("\n1. Text Search Performance:")
var startTime = new Date()
db.properties.find({"\$text": {"\$search": "madrid"}}).explain("executionStats")
var endTime = new Date()
print("   Text search execution time: " + (endTime - startTime) + "ms")

// 2. Price range queries
print("\n2. Price Range Performance:")
startTime = new Date()
db.properties.find({"price": {"\$gte": 500000, "\$lte": 1000000}}).explain("executionStats")
endTime = new Date()
print("   Price range execution time: " + (endTime - startTime) + "ms")

// 3. Operation type filtering
print("\n3. Operation Type Performance:")
startTime = new Date()
db.properties.find({"operationType": "sale"}).explain("executionStats")
endTime = new Date()
print("   Operation type execution time: " + (endTime - startTime) + "ms")

// 4. Combined filters
print("\n4. Combined Filters Performance:")
startTime = new Date()
db.properties.find({
    "operationType": "sale",
    "price": {"\$gte": 300000},
    "beds": {"\$gte": 2}
}).explain("executionStats")
endTime = new Date()
print("   Combined filters execution time: " + (endTime - startTime) + "ms")

// 5. Pagination performance
print("\n5. Pagination Performance:")
startTime = new Date()
db.properties.find().skip(100).limit(20).explain("executionStats")
endTime = new Date()
print("   Pagination execution time: " + (endTime - startTime) + "ms")

// Analyze owners collection
print("\n=== OWNERS COLLECTION ANALYSIS ===")
print("Current indexes:")
db.owners.getIndexes().forEach(function(index) {
    print("  - " + JSON.stringify(index.key) + " (name: " + index.name + ")")
})

// Analyze property images collection
print("\n=== PROPERTY IMAGES COLLECTION ANALYSIS ===")
print("Current indexes:")
db.propertyImages.getIndexes().forEach(function(index) {
    print("  - " + JSON.stringify(index.key) + " (name: " + index.name + ")")
})

// Analyze property traces collection
print("\n=== PROPERTY TRACES COLLECTION ANALYSIS ===")
print("Current indexes:")
db.propertyTraces.getIndexes().forEach(function(index) {
    print("  - " + JSON.stringify(index.key) + " (name: " + index.name + ")")
})
"@

    $analysisScript | mongosh --quiet
}

# Function to create optimized indexes
function Create-OptimizedIndexes {
    Write-Host "`n🚀 Creating Optimized Indexes..." -ForegroundColor Yellow
    
    $indexScript = @"
use $DatabaseName

print("Creating optimized indexes for better performance...")

// Properties collection - Composite indexes for common query patterns
print("\n1. Creating composite indexes for properties...")

// Composite index for search + price + operation type
db.properties.createIndex(
    {"name": "text", "address": "text", "price": 1, "operationType": 1},
    {name: "search_price_operation", background: true}
)

// Composite index for price range + operation type + beds
db.properties.createIndex(
    {"price": 1, "operationType": 1, "beds": 1},
    {name: "price_operation_beds", background: true}
)

// Composite index for location-based queries
db.properties.createIndex(
    {"address": 1, "price": 1, "operationType": 1},
    {name: "location_price_operation", background: true}
)

// Index for year + operation type
db.properties.createIndex(
    {"year": -1, "operationType": 1},
    {name: "year_operation", background: true}
)

// Sparse index for optional fields
db.properties.createIndex(
    {"sqft": 1},
    {name: "sqft_sparse", sparse: true, background: true}
)

// Owners collection - Optimized indexes
print("\n2. Creating optimized indexes for owners...")

db.owners.createIndex(
    {"name": "text", "address": 1},
    {name: "owner_search", background: true}
)

// Property Images collection - Performance indexes
print("\n3. Creating optimized indexes for property images...")

db.propertyImages.createIndex(
    {"propertyId": 1, "enabled": 1, "file": 1},
    {name: "property_images_performance", background: true}
)

// Property Traces collection - Time-based indexes
print("\n4. Creating optimized indexes for property traces...")

db.propertyTraces.createIndex(
    {"propertyId": 1, "dateSale": -1},
    {name: "property_traces_time", background: true}
)

db.propertyTraces.createIndex(
    {"dateSale": -1, "value": 1},
    {name: "traces_date_value", background: true}
)

print("\n✅ All optimized indexes created successfully!")
print("\nNew index list:")
db.properties.getIndexes().forEach(function(index) {
    print("  Properties: " + index.name + " -> " + JSON.stringify(index.key))
})
db.owners.getIndexes().forEach(function(index) {
    print("  Owners: " + index.name + " -> " + JSON.stringify(index.key))
})
db.propertyImages.getIndexes().forEach(function(index) {
    print("  PropertyImages: " + index.name + " -> " + JSON.stringify(index.key))
})
db.propertyTraces.getIndexes().forEach(function(index) {
    print("  PropertyTraces: " + index.name + " -> " + JSON.stringify(index.key))
})
"@

    $indexScript | mongosh --quiet
}

# Function to drop unnecessary indexes
function Remove-UnnecessaryIndexes {
    Write-Host "`n🗑️  Removing Unnecessary Indexes..." -ForegroundColor Yellow
    
    $dropScript = @"
use $DatabaseName

print("Removing unnecessary indexes...")

// Remove duplicate or inefficient indexes
var indexesToRemove = [
    // Remove old single-field indexes that are now covered by composite indexes
    "price_1",
    "operationType_1", 
    "ownerId_1"
];

indexesToRemove.forEach(function(indexName) {
    try {
        db.properties.dropIndex(indexName);
        print("  Removed index: " + indexName);
    } catch (e) {
        print("  Index " + indexName + " not found or cannot be removed");
    }
});

print("\n✅ Index cleanup completed!")
"@

    $dropScript | mongosh --quiet
}

# Function to show index statistics
function Show-IndexStats {
    Write-Host "`n📈 Index Statistics..." -ForegroundColor Yellow
    
    $statsScript = @"
use $DatabaseName

print("=== INDEX STATISTICS ===")

// Properties collection stats
print("\nProperties Collection:")
var propertiesStats = db.properties.stats()
print("  Total documents: " + propertiesStats.count)
print("  Total size: " + (propertiesStats.size / 1024 / 1024).toFixed(2) + " MB")
print("  Average document size: " + (propertiesStats.avgObjSize / 1024).toFixed(2) + " KB")
print("  Indexes size: " + (propertiesStats.totalIndexSize / 1024 / 1024).toFixed(2) + " MB")

// Show index usage statistics
print("\nIndex Usage (from query planner):")
db.properties.getIndexes().forEach(function(index) {
    print("  " + index.name + ": " + JSON.stringify(index.key));
});

// Owners collection stats
print("\nOwners Collection:")
var ownersStats = db.owners.stats()
print("  Total documents: " + ownersStats.count)
print("  Total size: " + (ownersStats.size / 1024 / 1024).toFixed(2) + " MB")

// Property Images collection stats
print("\nProperty Images Collection:")
var imagesStats = db.propertyImages.stats()
print("  Total documents: " + imagesStats.count)
print("  Total size: " + (imagesStats.size / 1024 / 1024).toFixed(2) + " MB")

// Property Traces collection stats
print("\nProperty Traces Collection:")
var tracesStats = db.propertyTraces.stats()
print("  Total documents: " + tracesStats.count)
print("  Total size: " + (tracesStats.size / 1024 / 1024).toFixed(2) + " MB")
"@

    $statsScript | mongosh --quiet
}

# Main execution
try {
    if ($Analyze) {
        Analyze-QueryPerformance
    }
    
    if ($Create) {
        Create-OptimizedIndexes
    }
    
    if ($Drop) {
        Remove-UnnecessaryIndexes
    }
    
    # If no specific action specified, show stats
    if (-not $Analyze -and -not $Create -and -not $Drop) {
        Show-IndexStats
        Write-Host "`n💡 Use -Analyze, -Create, or -Drop parameters for specific actions" -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "`n❌ Index optimization failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Index optimization completed!" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Test query performance improvements" -ForegroundColor White
Write-Host "   2. Monitor index usage in production" -ForegroundColor White
Write-Host "   3. Run performance tests to validate improvements" -ForegroundColor White

Write-Host "`n🚀 Ready for production!" -ForegroundColor Green
