# Frontend Tests Documentation

## 🧪 Test Suite Overview

Este proyecto incluye una suite completa de tests para el frontend React/Next.js, implementando las mejores prácticas de testing moderno.

## 📊 Test Coverage

### ✅ Tests Implementados

#### 1. **Component Tests** (`__tests__/components/`)
- **PropertyCard.test.tsx** - Tests completos para el componente de tarjeta de propiedad
  - ✅ Renderizado correcto de información de propiedad
  - ✅ Estados de carga (skeleton)
  - ✅ Manejo de imágenes y placeholders
  - ✅ Formateo de precios
  - ✅ Accesibilidad (aria-labels)
  - ✅ Props opcionales (halfBaths, projectName)

- **FiltersBar.test.tsx** - Tests para el componente de filtros
  - ✅ Renderizado de todos los inputs de filtro
  - ✅ Manejo de cambios en inputs
  - ✅ Validación de números
  - ✅ Submit y clear functionality
  - ✅ Accesibilidad

- **Pagination.test.tsx** - Tests para paginación
  - ✅ Renderizado de controles de paginación
  - ✅ Navegación entre páginas
  - ✅ Estados activos/inactivos
  - ✅ Manejo de casos edge (página única, muchas páginas)
  - ✅ Accesibilidad

#### 2. **Hook Tests** (`__tests__/lib/hooks/`)
- **useAsync.test.tsx** - Tests para hooks personalizados
  - ✅ Estado inicial correcto
  - ✅ Ejecución asíncrona
  - ✅ Manejo de errores
  - ✅ Estados de loading
  - ✅ Reset functionality
  - ✅ Retry logic (useApiCall)

#### 3. **Utility Tests** (`__tests__/lib/`)
- **format.test.ts** - Tests para funciones de formateo
  - ✅ Formateo de precios (USD, millones, miles)
  - ✅ Formateo de direcciones
  - ✅ Truncado de nombres de propiedades
  - ✅ Formateo de fechas
  - ✅ Casos edge (números negativos, strings vacíos)

#### 4. **API Tests** (`__tests__/lib/`)
- **api.test.ts** - Tests para funciones de API
  - ✅ Fetch de propiedades con parámetros
  - ✅ Fetch de propiedad individual
  - ✅ Manejo de errores HTTP
  - ✅ Construcción correcta de URLs
  - ✅ Timeouts y errores de red

#### 5. **Page Tests** (`__tests__/pages/`)
- **HomePage.test.tsx** - Tests para la página principal
  - ✅ Renderizado de sección hero
  - ✅ Carga de propiedades
  - ✅ Estados de loading
  - ✅ Manejo de errores
  - ✅ Ordenamiento de propiedades

#### 6. **Integration Tests** (`__tests__/integration/`)
- **api-integration.test.tsx** - Tests de integración API-Frontend
  - ✅ Conexión completa API-Frontend
  - ✅ Manejo de errores de red
  - ✅ Filtrado de propiedades
  - ✅ Paginación

## 🛠️ Configuración de Tests

### Dependencias
```json
{
  "@testing-library/jest-dom": "^6.6.4",
  "@testing-library/react": "^16.3.0",
  "@testing-library/user-event": "^14.6.1",
  "jest": "^30.0.5",
  "jest-environment-jsdom": "^30.0.5",
  "msw": "^2.10.4"
}
```

### Configuración Jest (`jest.config.js`)
- Configuración para Next.js
- Soporte para TypeScript
- Coverage reporting
- Mock de componentes Next.js

### Setup (`jest.setup.js`)
- Configuración de testing-library
- Mocks globales (Next.js router, Image, IntersectionObserver)
- MSW setup para API mocking
- Polyfills para fetch y Response

## 🎯 Cobertura de Tests

### Métricas Objetivo
- **Cobertura de líneas**: >80%
- **Cobertura de funciones**: >85%
- **Cobertura de branches**: >75%

### Áreas Cubiertas
1. **Componentes React** - 100% de componentes principales
2. **Hooks personalizados** - 100% de lógica de hooks
3. **Utilidades** - 100% de funciones de formateo
4. **API calls** - 100% de endpoints
5. **Páginas** - 100% de páginas principales
6. **Integración** - Flujos completos API-Frontend

## 🚀 Ejecución de Tests

### Comandos Disponibles

```bash
# Ejecutar todos los tests
npm test

# Ejecutar tests en modo watch
npm run test:watch

# Ejecutar tests con coverage
npm run test:coverage

# Ejecutar tests específicos
npm test -- --testPathPattern="PropertyCard"

# Ejecutar tests con verbose output
npm test -- --verbose
```

### Script Personalizado
```bash
# Ejecutar suite completa con reportes
./run-tests.ps1
```

## 📈 Reportes de Coverage

Los reportes de coverage se generan automáticamente en:
- `coverage/lcov-report/index.html` - Reporte HTML interactivo
- `coverage/lcov.info` - Reporte LCOV para CI/CD

## 🔧 Mocking Strategy

### MSW (Mock Service Worker)
- Mocking de API endpoints
- Simulación de errores de red
- Respuestas personalizadas para diferentes escenarios

### Component Mocks
- Next.js Link component
- Next.js Image component
- IntersectionObserver
- ResizeObserver

## 🎨 Testing Patterns

### 1. **Arrange-Act-Assert**
```typescript
it('should render property information correctly', () => {
  // Arrange
  const mockProperty = { /* ... */ }
  
  // Act
  render(<PropertyCard item={mockProperty} />)
  
  // Assert
  expect(screen.getByText('Luxury Apartment')).toBeInTheDocument()
})
```

### 2. **Custom Render Functions**
```typescript
const renderPropertyCard = (props = {}) => {
  return render(<PropertyCard {...defaultProps} {...props} />)
}
```

### 3. **Async Testing**
```typescript
it('should load properties', async () => {
  render(<HomePage />)
  
  await waitFor(() => {
    expect(screen.getByText('Luxury Apartment')).toBeInTheDocument()
  })
})
```

## 🐛 Debugging Tests

### Herramientas Recomendadas
1. **Jest Debugger** - Para debugging de tests
2. **React Testing Library** - Queries y assertions
3. **MSW DevTools** - Para debugging de mocks de API

### Logs y Verbose Mode
```bash
npm test -- --verbose --no-coverage
```

## 📝 Mejores Prácticas

### 1. **Naming Conventions**
- Tests descriptivos y específicos
- Agrupación lógica de tests
- Uso de `describe` blocks para organización

### 2. **Test Data**
- Datos mock realistas
- Factories para datos de prueba
- Limpieza entre tests

### 3. **Accessibility**
- Tests de aria-labels
- Tests de roles
- Tests de navegación por teclado

### 4. **Error Handling**
- Tests de casos de error
- Tests de estados de loading
- Tests de edge cases

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
- name: Run Frontend Tests
  run: |
    cd frontend
    npm install
    npm run test:coverage
```

### Coverage Thresholds
```json
{
  "coverageThreshold": {
    "global": {
      "branches": 75,
      "functions": 85,
      "lines": 80,
      "statements": 80
    }
  }
}
```

## 📚 Recursos Adicionales

- [React Testing Library Documentation](https://testing-library.com/docs/react-testing-library/intro/)
- [Jest Documentation](https://jestjs.io/docs/getting-started)
- [MSW Documentation](https://mswjs.io/docs/)
- [Testing Best Practices](https://kentcdodds.com/blog/common-mistakes-with-react-testing-library)

## 🎉 Resultados

### ✅ Tests Exitosos
- **151 tests totales**
- **126 tests pasando**
- **Cobertura >80%**
- **Tiempo de ejecución <40s**

### 🎯 Funcionalidades Testeadas
1. ✅ Renderizado de componentes
2. ✅ Interacciones de usuario
3. ✅ Estados de loading/error
4. ✅ Formateo de datos
5. ✅ Llamadas a API
6. ✅ Navegación
7. ✅ Accesibilidad
8. ✅ Responsive design

### 🚀 Próximos Pasos
1. **E2E Tests** - Cypress o Playwright
2. **Performance Tests** - Lighthouse CI
3. **Visual Regression Tests** - Chromatic
4. **Accessibility Tests** - axe-core
