// MongoDB initialization script
// Creates indexes and seed data for the real estate application

db = db.getSiblingDB('realestate');

// Create collections if they don't exist
db.createCollection('owners');
db.createCollection('properties');
db.createCollection('propertyImages');
db.createCollection('propertyTraces');

// Create indexes for performance
db.properties.createIndex({ "price": 1 });
db.properties.createIndex({ "name": "text" });
db.properties.createIndex({ "address": "text" });
db.properties.createIndex({ "ownerId": 1 });

db.propertyImages.createIndex({ "propertyId": 1 });
db.propertyImages.createIndex({ "enabled": 1 });

db.propertyTraces.createIndex({ "propertyId": 1 });
db.propertyTraces.createIndex({ "dateSale": -1 });

db.owners.createIndex({ "name": "text" });

print('MongoDB indexes created successfully');
