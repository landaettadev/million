# 🚀 Quick Setup Guide - Million Real Estate

## 📋 Prerequisites
- .NET 8 SDK
- Node.js 18+
- Docker Desktop

## ⚡ Setup in 5 Minutes

### 1. Clone and Navigate
```bash
git clone https://github.com/landaettadev/million.git
cd million
```

### 2. Start Services
```bash
# Start MongoDB and Azurite
docker-compose up -d

# Verify containers are running
docker ps
```

### 3. Configure Backend
```bash
cd backend/RealEstate.Api

# Copy and edit configuration
cp appsettings.Development.json.example appsettings.Development.json

# Run backend
dotnet restore
dotnet run
```

### 4. Configure Frontend
```bash
cd ../../frontend

# Copy and edit environment
cp env.example .env.local

# Install and run
npm install
npm run dev
```

### 5. Access URLs
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5244
- **Swagger**: http://localhost:5244/swagger

## 🔧 Configuration Files

### Backend: `appsettings.Development.json`
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://admin:password123@localhost:27017/realestate_dev?authSource=admin",
    "Database": "realestate_dev"
  }
}
```

### Frontend: `.env.local`
```bash
NEXT_PUBLIC_API_BASE=http://localhost:5244
NEXT_PUBLIC_IMAGE_BASE_URL=https://millionstorageprod.blob.core.windows.net/property-images
NEXT_PUBLIC_VIDEO_BASE_URL=http://localhost:3000
```

## 🎥 Videos Included
- `lujosa1.mp4` - Luxury Penthouse Madrid
- `lujosa2.mp4` - Modern Apartment Barcelona

## 🖼️ Images
- Azure Blob Storage integration
- No placeholder images (picsum removed)

## 🚨 Troubleshooting
- **Error 500**: Check if MongoDB is running (`docker ps`)
- **Images not loading**: Verify Azure Blob Storage connection
- **Videos not playing**: Check if videos exist in `/frontend/public/`

## 📞 Support
- Check logs: `docker logs realestate-mongodb`
- Backend logs: Check terminal running `dotnet run`
- Frontend logs: Check browser console
