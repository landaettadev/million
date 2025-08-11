# 🚀 Installation Guide - Million Real Estate

This guide will walk you through the complete installation and setup of the Million Real Estate application.

## 📋 System Requirements

### Minimum Requirements
- **OS**: Windows 10/11, macOS 10.15+, or Ubuntu 18.04+
- **RAM**: 8GB RAM
- **Storage**: 10GB free space
- **Network**: Internet connection for package downloads

### Recommended Requirements
- **OS**: Windows 11, macOS 12+, or Ubuntu 20.04+
- **RAM**: 16GB RAM
- **Storage**: 20GB free space (SSD recommended)
- **Network**: High-speed internet connection

## 🛠️ Prerequisites Installation

### 1. .NET 8 SDK
```bash
# Windows
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Run the installer and follow the wizard

# macOS
brew install dotnet

# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get install -y dotnet-sdk-8.0

# Verify installation
dotnet --version
# Should display: 8.0.x
```

### 2. Node.js 18+
```bash
# Windows
# Download from: https://nodejs.org/
# Run the installer and follow the wizard

# macOS
brew install node

# Ubuntu/Debian
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Verify installation
node --version
npm --version
```

### 3. MongoDB
```bash
# Option A: Docker (Recommended)
docker --version
# If not installed, download from: https://www.docker.com/

# Option B: Local Installation
# Windows: Download from https://www.mongodb.com/try/download/community
# macOS: brew install mongodb-community
# Ubuntu: sudo apt-get install mongodb
```

## 📥 Project Setup

### 1. Clone Repository
```bash
git clone https://github.com/landaettadev/million.git
cd million
```

### 2. Backend Configuration
```bash
cd backend/RealEstate.Api

# Copy configuration template
cp appsettings.Development.json.example appsettings.Development.json

# Edit configuration file
# Update MongoDB connection string and other settings
```

**Configuration Example:**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017/million",
    "DatabaseName": "million"
  },
  "JWT": {
    "SecretKey": "your-super-secret-key-here-minimum-32-characters-long",
    "Issuer": "million-real-estate",
    "Audience": "million-users"
  }
}
```

### 3. Frontend Configuration
```bash
cd frontend

# Copy environment template
cp env.example .env.local

# Edit environment file
# Update API URLs and other settings
```

**Environment Variables:**
```env
NEXT_PUBLIC_API_URL=http://localhost:5244
NEXT_PUBLIC_IMAGE_BASE_URL=https://your-storage-account.blob.core.windows.net/property-images
```

## 🗄️ Database Setup

### Option A: Docker MongoDB
```bash
# Start MongoDB container
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Verify container is running
docker ps

# Access MongoDB shell (optional)
docker exec -it mongodb mongosh
```

### Option B: MongoDB Atlas (Cloud)
1. Go to [MongoDB Atlas](https://www.mongodb.com/atlas)
2. Create free account
3. Create new cluster
4. Get connection string
5. Update `appsettings.Development.json`

### Option C: Local MongoDB Installation
```bash
# Start MongoDB service
sudo systemctl start mongod
sudo systemctl enable mongod

# Verify service status
sudo systemctl status mongod
```

## 🚀 Running the Application

### 1. Start Backend
```bash
cd backend/RealEstate.Api

# Restore packages
dotnet restore

# Run the application
dotnet run

# API will be available at:
# - https://localhost:7244 (HTTPS)
# - http://localhost:5244 (HTTP)
```

### 2. Start Frontend
```bash
cd frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Frontend will be available at:
# - http://localhost:3000
```

### 3. Verify Installation
- **Backend**: Visit `https://localhost:7244/health`
- **Frontend**: Visit `http://localhost:3000`
- **Database**: Check MongoDB connection

## 🧪 Running Tests

### All Tests (Recommended)
```bash
# From project root
./run-all-tests.ps1  # Windows PowerShell
# or
pwsh run-all-tests.ps1  # Cross-platform
```

### Individual Test Suites
```bash
# Backend Tests
cd backend
dotnet test

# Frontend Tests
cd frontend
npm test

# Integration Tests
cd backend
dotnet test RealEstate.Tests.Integration

# Performance Tests
cd backend
dotnet test RealEstate.Tests.Performance
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Port Already in Use
```bash
# Check what's using the port
netstat -ano | findstr :5244  # Windows
lsof -i :5244  # macOS/Linux

# Kill the process or change port in configuration
```

#### 2. MongoDB Connection Failed
```bash
# Check MongoDB status
docker ps  # If using Docker
sudo systemctl status mongod  # If using local installation

# Verify connection string
# Test connection manually
```

#### 3. .NET SDK Not Found
```bash
# Verify .NET installation
dotnet --version

# Add to PATH if necessary
# Restart terminal/command prompt
```

#### 4. Node.js Dependencies Issues
```bash
# Clear npm cache
npm cache clean --force

# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Performance Issues
- Ensure sufficient RAM (8GB+)
- Use SSD storage if possible
- Close unnecessary applications
- Check network connectivity

## 📱 Accessing the Application

### Default Admin Account
- **Email**: admin@million.com
- **Password**: Admin123!

### API Endpoints
- **Health Check**: `GET /health`
- **Properties**: `GET /api/properties`
- **Admin Login**: `POST /api/admin/auth/login`

### Frontend Routes
- **Home**: `/`
- **Properties**: `/properties`
- **Property Detail**: `/properties/{id}`
- **Admin Panel**: `/admin`

## 🔒 Security Considerations

### Development Environment
- Use strong JWT secret keys
- Enable HTTPS in production
- Configure CORS properly
- Use environment variables for secrets

### Production Deployment
- Change default admin credentials
- Use secure MongoDB connections
- Enable authentication on all endpoints
- Implement rate limiting

## 📊 Monitoring and Logs

### Backend Logs
```bash
# View application logs
# Check console output when running dotnet run

# Log files location (if configured)
# Check appsettings.json for logging configuration
```

### Frontend Logs
```bash
# Browser Developer Tools
# Console tab for JavaScript errors
# Network tab for API calls
```

### Database Monitoring
```bash
# MongoDB Compass (GUI)
# MongoDB shell commands
# Performance monitoring tools
```

## 🚀 Next Steps

After successful installation:

1. **Explore the API**: Use Swagger UI at `/swagger`
2. **Test the Frontend**: Navigate through all pages
3. **Run Tests**: Ensure all tests pass
4. **Customize**: Modify configuration for your needs
5. **Deploy**: Prepare for production deployment

## 📞 Support

If you encounter issues:

1. Check this installation guide
2. Review the [README.md](README.md)
3. Check [GitHub Issues](https://github.com/landaettadev/million/issues)
4. Contact the development team

---

**Happy coding! 🎉**
