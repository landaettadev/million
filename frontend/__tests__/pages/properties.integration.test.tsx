import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PropertiesPage from '../../app/properties/page'
import { server } from '../setup/server'
import { http, HttpResponse } from 'msw'

// Mock the PropertyCarousel component to simplify testing
jest.mock('../../components/property/PropertyCarousel', () => ({
  PropertyCarousel: ({ items }: { items: any[] }) => (
    <div data-testid="property-carousel">
      {items.map((item) => (
        <div key={item.id} data-testid={`property-${item.id}`}>
          <h3>{item.name}</h3>
          <p>{item.address}</p>
          <p>${item.price.toLocaleString()}</p>
        </div>
      ))}
    </div>
  )
}))

describe('Properties Page Integration', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
  afterEach(() => server.resetHandlers())
  afterAll(() => server.close())

  beforeEach(() => {
    // Set URL to same-origin path to avoid JSDOM security error
    window.history.pushState({}, '', '/properties')
  })

  it('loads and displays properties on initial render', async () => {
    render(<PropertiesPage />)

    expect(screen.getByText('Recent Transactions')).toBeInTheDocument()
    
    // Wait for properties to load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(screen.getByText('Modern Apartment')).toBeInTheDocument()
    expect(screen.getByText('123 Park Avenue, New York')).toBeInTheDocument()
    expect(screen.getByText('456 5th Avenue, New York')).toBeInTheDocument()
  })

  it('filters properties by name', async () => {
    const user = userEvent.setup()
    render(<PropertiesPage />)

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    // Find and fill the name filter
    const nameInput = screen.getByLabelText(/name/i) || screen.getByPlaceholderText(/name/i)
    await user.type(nameInput, 'Luxury')

    // Submit the form
    const searchButton = screen.getByRole('button', { name: /search/i })
    await user.click(searchButton)

    // Wait for filtered results
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
      expect(screen.queryByText('Modern Apartment')).not.toBeInTheDocument()
    })
  })

  it('handles API errors gracefully', async () => {
    // Mock API error
    server.use(
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

    render(<PropertiesPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
      expect(screen.getByText(/server error/i)).toBeInTheDocument()
    })

    // Check retry button is present
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument()
  })

  it('retries API call when retry button is clicked', async () => {
    let callCount = 0
    
    // Mock API to fail first time, succeed second time
    server.use(
      http.get('http://localhost:5244/api/properties', () => {
        callCount++
        if (callCount === 1) {
          return HttpResponse.json(
            {
              traceId: 'test-trace-id',
              error: 'Internal server error',
              statusCode: 500,
              timestamp: new Date().toISOString(),
            },
            { status: 500 }
          )
        }
        
        return HttpResponse.json({
          items: [
            {
              id: '507f1f77bcf86cd799439011',
              idOwner: '507f1f77bcf86cd799439012',
              name: 'Luxury Penthouse',
              address: '123 Park Avenue, New York',
              price: 2500000,
              image: 'https://picsum.photos/800/600?random=1',
              operationType: 'sale',
              beds: 3,
              baths: 2,
              halfBaths: 1,
              sqft: 2500,
            }
          ],
          page: 1,
          pageSize: 20,
          total: 1,
        })
      })
    )

    const user = userEvent.setup()
    render(<PropertiesPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
    })

    // Click retry button
    const retryButton = screen.getByRole('button', { name: /try again/i })
    await user.click(retryButton)

    // Wait for successful retry
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(callCount).toBe(2)
  })

  it('displays no properties message when no results', async () => {
    // Mock empty results
    server.use(
      http.get('http://localhost:5244/api/properties', () => {
        return HttpResponse.json({
          items: [],
          page: 1,
          pageSize: 20,
          total: 0,
        })
      })
    )

    render(<PropertiesPage />)

    await waitFor(() => {
      expect(screen.getByText('No properties found')).toBeInTheDocument()
      expect(screen.getByText(/try adjusting your filters/i)).toBeInTheDocument()
    })
  })

  it('clears filters correctly', async () => {
    const user = userEvent.setup()
    render(<PropertiesPage />)

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    // Fill filters
    const nameInput = screen.getByLabelText(/name/i) || screen.getByPlaceholderText(/name/i)
    await user.type(nameInput, 'Luxury')

    // Clear filters
    const clearButton = screen.getByRole('button', { name: /clear/i })
    await user.click(clearButton)

    // Check that input is cleared
    expect(nameInput).toHaveValue('')

    // Properties should be reloaded
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
      expect(screen.getByText('Modern Apartment')).toBeInTheDocument()
    })
  })

  it('shows loading state while fetching data', () => {
    render(<PropertiesPage />)

    // Should show loading skeletons initially
    expect(screen.getByTestId('property-carousel')).toBeInTheDocument()
    
    // Should show loading items (skeleton data)
    const loadingItems = screen.getAllByText('Loading')
    expect(loadingItems.length).toBeGreaterThan(0)
  })
})
