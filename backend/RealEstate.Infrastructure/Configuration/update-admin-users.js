// Script to update existing AdminUser documents with new fields
// Run this in MongoDB shell or MongoDB Compass

db.adminUsers.updateMany(
  { 
    $or: [
      { "RefreshTokens": { $exists: false } },
      { "CreatedAt": { $exists: false } },
      { "LastLoginAt": { $exists: false } }
    ]
  },
  {
    $set: {
      "RefreshTokens": [],
      "CreatedAt": new Date(),
      "LastLoginAt": null
    }
  }
);

// Create indexes for better performance
db.refreshTokens.createIndex({ "Token": 1 }, { unique: true });
db.refreshTokens.createIndex({ "UserId": 1 });
db.refreshTokens.createIndex({ "ExpiresAt": 1 }, { expireAfterSeconds: 0 });

db.tokenBlacklist.createIndex({ "Token": 1 }, { unique: true });
db.tokenBlacklist.createIndex({ "UserId": 1 });
db.tokenBlacklist.createIndex({ "ExpiresAt": 1 }, { expireAfterSeconds: 0 });

db.adminUsers.createIndex({ "Email": 1 }, { unique: true });
db.adminUsers.createIndex({ "RefreshTokens.Token": 1 });

print("AdminUser migration completed successfully!");
print("Indexes created for better performance.");
