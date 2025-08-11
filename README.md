# 🏠 Million Real Estate - Full Stack Application

A comprehensive real estate platform built with .NET 8, MongoDB, and Next.js, featuring property management, search capabilities, and a modern responsive interface.

## 🚀 Features

- **Property Management**: Full CRUD operations for real estate properties
- **Advanced Search**: Filter properties by name, address, price range, and more
- **Image Management**: Azure Blob Storage integration for property images
- **Responsive Design**: Modern UI that works on all devices
- **Admin Panel**: Complete administrative interface for property management
- **Authentication**: Secure admin access with JWT tokens
- **Real-time Updates**: Live property data updates

## 🛠️ Technology Stack

### Backend
- **.NET 8** - Modern C# framework
- **MongoDB** - NoSQL database
- **Azure Blob Storage** - Image storage service
- **JWT Authentication** - Secure API access
- **Clean Architecture** - Modular and maintainable code structure

### Frontend
- **Next.js 14** - React framework with App Router
- **TypeScript** - Type-safe development
- **Tailwind CSS** - Utility-first CSS framework
- **React Testing Library** - Component testing
- **Jest** - JavaScript testing framework

### Testing
- **NUnit** - Backend unit testing
- **Jest + React Testing Library** - Frontend testing
- **Integration Tests** - API endpoint testing

## 📋 Prerequisites

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **MongoDB** - Local installation or MongoDB Atlas account
- **Azure Account** - For Blob Storage (optional, can use local storage)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/landaettadev/million.git
cd million
```

### 2. Backend Setup

```bash
cd backend/RealEstate.Api

# Restore dependencies
dotnet restore

# Set up environment variables
cp appsettings.Development.json.example appsettings.Development.json
# Edit appsettings.Development.json with your MongoDB connection string

# Run the application
dotnet run
```

The API will be available at `https://localhost:7244` or `http://localhost:5244`

### 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Set up environment variables
cp .env.local.example .env.local
# Edit .env.local with your API endpoints

# Run the development server
npm run dev
```

The frontend will be available at `http://localhost:3000`

### 4. Database Setup

#### Option A: Local MongoDB
```bash
# Install MongoDB locally or use Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Connection string: mongodb://localhost:27017/million
```

#### Option B: MongoDB Atlas
1. Create a free account at [MongoDB Atlas](https://www.mongodb.com/atlas)
2. Create a new cluster
3. Get your connection string
4. Update `appsettings.Development.json`

### 5. Azure Blob Storage (Optional)

If you want to use Azure Blob Storage for images:

1. Create an Azure Storage Account
2. Create a container named `property-images`
3. Update the connection string in `appsettings.Development.json`
4. Or use local file storage (default)

## 🔧 Configuration

### Backend Configuration

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017/million",
    "DatabaseName": "million"
  },
  "AzureStorage": {
    "ConnectionString": "your-azure-connection-string",
    "ContainerName": "property-images"
  },
  "JWT": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "million-real-estate",
    "Audience": "million-users"
  }
}
```

### Frontend Configuration

```env
NEXT_PUBLIC_API_URL=http://localhost:5244
NEXT_PUBLIC_IMAGE_BASE_URL=https://your-storage-account.blob.core.windows.net/property-images
```

## 🧪 Running Tests

### Backend Tests
```bash
cd backend
dotnet test
```

### Frontend Tests
```bash
cd frontend
npm test
```

### All Tests
```bash
# From root directory
npm run test:all
```

## 📁 Project Structure

```
million/
├── backend/                          # .NET Backend
│   ├── RealEstate.Api/              # Main API project
│   ├── RealEstate.Application/       # Business logic layer
│   ├── RealEstate.Infrastructure/   # Data access layer
│   └── RealEstate.Tests*/           # Test projects
├── frontend/                         # Next.js Frontend
│   ├── app/                         # App Router pages
│   ├── components/                  # React components
│   ├── lib/                         # Utilities and API
│   └── __tests__/                   # Test files
├── infra/                           # Infrastructure as Code
│   └── terraform/                   # Azure resources
└── docs/                            # Documentation
```

## 🔌 API Endpoints

### Properties
- `GET /api/properties` - List all properties with pagination
- `GET /api/properties/{id}` - Get property by ID
- `GET /api/properties/featured` - Get featured properties

### Admin (Protected)
- `POST /api/admin/auth/login` - Admin login
- `POST /api/admin/properties` - Create property
- `PUT /api/admin/properties/{id}` - Update property
- `DELETE /api/admin/properties/{id}` - Delete property
- `POST /api/admin/images/upload` - Upload property image

## 🎨 Frontend Features

- **Homepage**: Featured properties and search
- **Properties List**: Paginated property grid with filters
- **Property Detail**: Comprehensive property information
- **Admin Panel**: Property management interface
- **Responsive Design**: Mobile-first approach

## 🚀 Deployment

### Backend Deployment
```bash
cd backend/RealEstate.Api
dotnet publish -c Release -o ./publish
```

### Frontend Deployment
```bash
cd frontend
npm run build
npm run start
```

### Docker Deployment
```bash
# Build and run with Docker Compose
docker-compose up -d
```

## 🔒 Security Features

- JWT-based authentication
- Role-based access control
- Secure password hashing with BCrypt
- CORS configuration
- Input validation and sanitization

## 📊 Performance Optimizations

- Database query optimization
- Image lazy loading
- Pagination for large datasets
- Caching strategies
- CDN integration for images

## 🧪 Testing Strategy

- **Unit Tests**: Individual component testing
- **Integration Tests**: API endpoint testing
- **E2E Tests**: Full user journey testing
- **Performance Tests**: Load testing for scalability

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

If you encounter any issues:

1. Check the [Issues](https://github.com/landaettadev/million/issues) page
2. Create a new issue with detailed information
3. Contact the development team

## 🎯 Roadmap

- [ ] Real-time notifications
- [ ] Advanced analytics dashboard
- [ ] Mobile app development
- [ ] AI-powered property recommendations
- [ ] Virtual tour integration
- [ ] Payment processing integration

---

**Built with ❤️ by the Million Real Estate Team**
