import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import { server } from '../setup/server'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { api } from '../../lib/api'

// Create a new QueryClient for testing
const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

// Mock Next.js components
jest.mock('next/link', () => {
  return ({ children, href, ...props }: any) => {
    return <a href={href} {...props}>{children}</a>
  }
})

jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: any) => {
    return <img src={src} alt={alt} {...props} />
  },
}))

// Test component that uses the API
const TestComponent = () => {
  const [properties, setProperties] = React.useState<any[]>([])
  const [loading, setLoading] = React.useState(true)
  const [error, setError] = React.useState<string | null>(null)

  React.useEffect(() => {
    const fetchProperties = async () => {
      try {
        setLoading(true)
        const result = await api.getProperties()
        setProperties(result.items)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error')
      } finally {
        setLoading(false)
      }
    }

    fetchProperties()
  }, [])

  if (loading) return <div>Loading...</div>
  if (error) return <div>Error: {error}</div>

  return (
    <div>
      {properties.map(property => (
        <div key={property.id} data-testid="property-card">
          <h3>{property.name}</h3>
          <p>{property.address}</p>
          <p>{property.price}</p>
        </div>
      ))}
    </div>
  )
}

describe('API Integration Tests', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
  afterEach(() => server.resetHandlers())
  afterAll(() => server.close())
  let queryClient: QueryClient

  beforeEach(() => {
    queryClient = createTestQueryClient()
  })

  afterEach(() => {
    queryClient.clear()
  })

  it('should fetch and display properties successfully', async () => {
    const mockProperties = [
      {
        id: '1',
        idOwner: 'owner1',
        name: 'Luxury Apartment',
        address: '123 Park Avenue, New York',
        price: 2500000,
        operationType: 'sale',
        image: 'https://example.com/image1.jpg'
      },
      {
        id: '2',
        idOwner: 'owner2',
        name: 'Modern Penthouse',
        address: '456 5th Avenue, New York',
        price: 3500000,
        operationType: 'sale',
        image: 'https://example.com/image2.jpg'
      }
    ]

    server.use(
      http.get('http://localhost:5244/api/properties', () => {
        return HttpResponse.json({
          items: mockProperties,
          total: mockProperties.length,
          page: 1,
          pageSize: 20
        })
      })
    )

    render(
      <QueryClientProvider client={queryClient}>
        <TestComponent />
      </QueryClientProvider>
    )

    // Should show loading initially
    expect(screen.getByText('Loading...')).toBeInTheDocument()

    // Should display properties after loading
    await waitFor(() => {
      expect(screen.getByText('Luxury Apartment')).toBeInTheDocument()
      expect(screen.getByText('Modern Penthouse')).toBeInTheDocument()
    })

    expect(screen.getByText('123 Park Avenue, New York')).toBeInTheDocument()
    expect(screen.getByText('456 5th Avenue, New York')).toBeInTheDocument()
  })

  it('should handle API errors gracefully', async () => {
    server?.use(
      http.get('http://localhost:5244/api/properties', () => {
        return HttpResponse.json(
          {
            traceId: 'test-trace-id',
            error: 'Internal server error',
            statusCode: 500,
            timestamp: new Date().toISOString(),
          },
          { status: 500 }
        )
      })
    )

    render(
      <QueryClientProvider client={queryClient}>
        <TestComponent />
      </QueryClientProvider>
    )

    await waitFor(() => {
      expect(screen.getByText(/Error:/)).toBeInTheDocument()
    })
  })

  it('should handle network errors', async () => {
    server.use(
      http.get('http://localhost:5244/api/properties', () => {
        return HttpResponse.error()
      })
    )

    render(
      <QueryClientProvider client={queryClient}>
        <TestComponent />
      </QueryClientProvider>
    )

    await waitFor(() => {
      expect(screen.getByText(/Error:/)).toBeInTheDocument()
    })
  })

  it('should handle empty property list', async () => {
    server.use(
      http.get('http://localhost:5244/api/properties', () => {
        return HttpResponse.json({
          items: [],
          total: 0,
          page: 1,
          pageSize: 20
        })
      })
    )

    render(
      <QueryClientProvider client={queryClient}>
        <TestComponent />
      </QueryClientProvider>
    )

    await waitFor(() => {
      expect(screen.queryByTestId('property-card')).not.toBeInTheDocument()
    })
  })

  it('should handle property filtering', async () => {
    const mockProperties = [
      {
        id: '1',
        idOwner: 'owner1',
        name: 'Luxury Apartment',
        address: '123 Park Avenue, New York',
        price: 2500000,
        operationType: 'sale',
        image: 'https://example.com/image1.jpg'
      }
    ]

    server.use(
      http.get('http://localhost:5244/api/properties', ({ request }) => {
        const url = new URL(request.url)
        const name = url.searchParams.get('name')
        
        if (name === 'Luxury') {
          return HttpResponse.json({
            items: mockProperties,
            total: mockProperties.length,
            page: 1,
            pageSize: 20
          })
        }
        
        return HttpResponse.json({
          items: [],
          total: 0,
          page: 1,
          pageSize: 20
        })
      })
    )

    // Test filtering by name
    const result = await api.getProperties({ name: 'Luxury' })
    
    expect(result.items).toHaveLength(1)
    expect(result.items[0].name).toBe('Luxury Apartment')
  })

  it('should handle pagination', async () => {
    const mockProperties = [
      {
        id: '2',
        idOwner: 'owner2',
        name: 'Modern Penthouse',
        address: '456 5th Avenue, New York',
        price: 3500000,
        operationType: 'sale',
        image: 'https://example.com/image2.jpg'
      }
    ]

    server.use(
      http.get('http://localhost:5244/api/properties', ({ request }) => {
        const url = new URL(request.url)
        const page = url.searchParams.get('page')
        
        if (page === '2') {
          return HttpResponse.json({
            items: mockProperties,
            total: 2,
            page: 2,
            pageSize: 1
          })
        }
        
        return HttpResponse.json({
          items: [],
          total: 2,
          page: 1,
          pageSize: 1
        })
      })
    )

    // Test pagination
    const result = await api.getProperties({ page: 2, pageSize: 1 })
    
    expect(result.page).toBe(2)
    expect(result.pageSize).toBe(1)
    expect(result.items).toHaveLength(1)
    expect(result.items[0].name).toBe('Modern Penthouse')
  })
})
