# Real Estate MVP - Million Luxury

MVP para consulta de propiedades inmobiliarias (venta y alquiler) con listado, filtros y detalle.

## 🏗️ Arquitectura

- **Backend**: .NET 8 Minimal API + MongoDB
- **Frontend**: Next.js 14 + React Query + Tailwind + shadcn/ui
- **Base de Datos**: MongoDB 7.0 (Docker Compose)

## 🚀 Inicio Rápido

### Prerrequisitos

- Docker Desktop
- .NET 8 SDK
- Node.js 18+

### 1. Base de Datos

```bash
# Iniciar MongoDB
docker-compose up -d
```

### 2. Backend

```bash
cd backend/RealEstate.Api
dotnet restore
dotnet run
```

El API estará disponible en: http://localhost:5000
Swagger UI: http://localhost:5000/swagger

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

El frontend estará disponible en: http://localhost:3000

## 📋 Funcionalidades

### Listado de Propiedades
- Filtros: nombre, dirección, rango de precio, tipo de operación
- Paginación
- Ordenamiento por precio ascendente

### Detalle de Propiedad
- Información completa
- Galería de imágenes
- Navegación responsiva

## 🗄️ Modelo de Datos

### Collections MongoDB

```json
// Owner Collection
Owner {
  _id: ObjectId,
  name: string,
  address: string,
  photo: string,
  birthday: Date
}

// Property Collection
Property {
  _id: ObjectId,
  ownerId: ObjectId,  // FK a Owner
  name: string,
  address: string,
  price: number,
  codeInternal?: string,
  year?: number,
  operationType: "sale" | "rent"
}

// PropertyImage Collection
PropertyImage {
  _id: ObjectId,
  propertyId: ObjectId,  // FK a Property
  file: string,
  enabled: boolean
}

// PropertyTrace Collection
PropertyTrace {
  _id: ObjectId,
  propertyId: ObjectId,  // FK a Property
  dateSale: Date,
  name: string,
  value: number,
  tax: number
}
```

## 🔧 Endpoints

- `GET /api/properties` - Listado con filtros y paginación
- `GET /api/properties/{id}` - Detalle de propiedad

## 🧪 Tests

### Unit Tests
```bash
# Backend Unit Tests
cd backend/RealEstate.Tests
dotnet test

# Frontend Unit Tests
cd frontend
npm test
```

### Integration Tests
```bash
# Endpoint Integration Tests (uses Testcontainers)
cd backend/RealEstate.Tests.Integration
dotnet test
```

### Performance Tests
```bash
# Benchmark Tests
cd backend/RealEstate.Tests.Performance
dotnet run --configuration Release

# Load Tests
cd backend/RealEstate.Tests.Performance
dotnet test --filter "Category=LoadTest"
```

### Run All Tests
```bash
# Windows
cd backend
.\run-all-tests.ps1

# Linux/Mac
cd backend
./run-all-tests.sh
```

## 📁 Estructura del Proyecto

```
million/
├── backend/
│   ├── RealEstate.Api/          # Minimal API, Endpoints, Program.cs
│   │   ├── Endpoints/           # API endpoints
│   │   ├── Extensions/          # Service extensions
│   │   └── Middleware/          # Global middleware
│   ├── RealEstate.Application/   # DTOs, Services, Interfaces
│   │   ├── DTOs/                # Data Transfer Objects
│   │   ├── Services/            # Business logic
│   │   ├── Interfaces/          # Service contracts
│   │   └── Exceptions/          # Custom exceptions
│   ├── RealEstate.Infrastructure/ # MongoDB repos, models
│   │   ├── Repositories/        # Data access layer
│   │   ├── Models/              # MongoDB models
│   │   ├── Data/                # Database context
│   │   ├── Configuration/       # MongoDB config
│   │   └── Seeders/             # Data seeding
│   ├── RealEstate.Tests/        # NUnit unit tests
│   │   ├── Services/            # Service tests
│   │   └── Repositories/        # Repository tests
│   ├── RealEstate.Tests.Integration/ # Integration tests
│   │   ├── Endpoints/           # API endpoint tests
│   │   └── Infrastructure/      # Test infrastructure
│   └── RealEstate.Tests.Performance/ # Performance & load tests
│       ├── Benchmarks/          # BenchmarkDotNet tests
│       ├── LoadTests/           # NBomber load tests
│       └── Infrastructure/      # Performance test setup
├── frontend/
│   ├── app/(routes)/            # Next.js App Router
│   │   └── properties/          # Property pages
│   │       └── [id]/            # Property detail
│   ├── components/              # React components
│   │   ├── ui/                  # shadcn/ui components
│   │   ├── properties/          # Property components
│   │   │   ├── PropertyCard/    # Property card component
│   │   │   └── PropertyDetail/  # Property detail component
│   │   ├── filters/             # Filter components
│   │   └── forms/               # Form components
│   ├── lib/                     # Utilities
│   │   ├── api/                 # API client
│   │   ├── hooks/               # Custom hooks
│   │   ├── types/               # TypeScript types
│   │   └── utils/               # Utility functions
│   └── styles/                  # Global styles
├── docker-compose.yml           # MongoDB setup
└── README.md
```

## 🌱 Semilla Automática

Al iniciar el backend, se insertarán automáticamente 12 propiedades de ejemplo si la colección está vacía.

## 🔍 Índices MongoDB

### Properties Collection
- `price: 1` - Ordenamiento por precio
- `name: text` - Búsqueda por nombre
- `address: text` - Búsqueda por dirección
- `ownerId: 1` - Relación con Owner

### PropertyImages Collection
- `propertyId: 1` - Relación con Property
- `enabled: 1` - Filtro de imágenes habilitadas

### PropertyTraces Collection
- `propertyId: 1` - Relación con Property
- `dateSale: -1` - Ordenamiento por fecha de venta

### Owners Collection
- `name: text` - Búsqueda por nombre del propietario

## 📱 Características Técnicas

- **Rendimiento**: Respuestas <300ms local
- **Cache**: React Query para optimización frontend
- **Responsive**: Grid adaptativo mobile/desktop
- **Error Handling**: Middleware global con envelope
- **CORS**: Configurado para frontend
- **Swagger**: Documentación automática en desarrollo

## 🧪 Testing Strategy

### Niveles de Testing
- **Unit Tests**: NUnit (backend) + Jest/RTL (frontend)
- **Integration Tests**: WebApplicationFactory + Testcontainers
- **Performance Tests**: BenchmarkDotNet para métricas precisas
- **Load Tests**: NBomber para pruebas bajo carga
- **End-to-End**: Cypress (futuro)

### Métricas de Performance
- **Response Time**: <500ms promedio bajo carga normal
- **Throughput**: >100 req/s con 50 usuarios concurrentes
- **Error Rate**: <1% bajo carga normal, <5% bajo estrés
- **Memory Usage**: Monitoreo de memory leaks
- **Database**: Optimización de queries y índices
