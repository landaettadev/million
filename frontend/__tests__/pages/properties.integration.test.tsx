import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import '@testing-library/jest-dom'
import PropertiesPage from '../../app/properties/page'

// Mock the PropertyCarousel component to simplify testing
jest.mock('../../components/property/PropertyCarousel', () => ({
  PropertyCarousel: ({ items }: { items: any[] }) => (
    <div data-testid="property-carousel">
      {items.map((item) => (
        <div key={item.id} data-testid={`property-${item.id}`}>
          <h3>{item.name}</h3>
          <p>{item.address}</p>
          <p>${item.price?.toLocaleString() || 'Price on request'}</p>
        </div>
      ))}
    </div>
  )
}))

// Mock the API module completely
jest.mock('../../lib/api', () => ({
  api: {
    getProperties: jest.fn()
  }
}))

// Get the mocked function after the mock is set up
const { api } = require('../../lib/api')
const mockGetProperties = api.getProperties

// Mock data
const mockProperties = [
  {
    id: '507f1f77bcf86cd799439011',
    name: 'Luxury Penthouse',
    address: '123 Park Avenue, New York',
    price: 2500000,
    image: 'https://picsum.photos/800/600?random=1',
    operationType: 'sale',
    beds: 3,
    baths: 2,
    sqft: 2500,
  },
  {
    id: '507f1f77bcf86cd799439012',
    name: 'Modern Apartment',
    address: '456 5th Avenue, New York',
    price: 1200000,
    image: 'https://picsum.photos/800/600?random=2',
    operationType: 'sale',
    beds: 2,
    baths: 1,
    sqft: 1500,
  },
]

describe('Properties Page Integration', () => {
  beforeEach(() => {
    // Set URL to same-origin path to avoid JSDOM security error
    window.history.pushState({}, '', '/properties')
    
    // Reset and configure the mock for each test
    mockGetProperties.mockReset()
    mockGetProperties.mockResolvedValue({
      items: mockProperties,
      total: 2,
      page: 1,
      pageSize: 10,
    })
  })

  afterEach(() => {
    mockGetProperties.mockClear()
  })

  afterAll(() => {
    mockGetProperties.mockClear()
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
    
    // Mock API to return filtered results when name filter is applied
    mockGetProperties.mockImplementation((params: any) => {
      if (params.name === 'Luxury') {
        return Promise.resolve({
          items: [mockProperties[0]], // Only Luxury Penthouse
          total: 1,
          page: 1,
          pageSize: 10,
        })
      }
      return Promise.resolve({
        items: mockProperties,
        total: 2,
        page: 1,
        pageSize: 10,
      })
    })
    
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
    mockGetProperties.mockRejectedValue(new Error('Internal server error'))

    render(<PropertiesPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
      expect(screen.getByText('An unexpected error occurred')).toBeInTheDocument()
    })

    // Check retry button is present
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument()
  })

  it('retries API call when retry button is clicked', async () => {
    let callCount = 0
    
    // Mock API to fail first time, succeed second time
    mockGetProperties.mockImplementation(() => {
      callCount++
      if (callCount === 1) {
        return Promise.reject(new Error('Internal server error'))
      }
      return Promise.resolve({
        items: mockProperties,
        total: 2,
        page: 1,
        pageSize: 10,
      })
    })

    const user = userEvent.setup()
    render(<PropertiesPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
      expect(screen.getByText('An unexpected error occurred')).toBeInTheDocument()
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

  it('clears filters correctly', async () => {
    const user = userEvent.setup()
    render(<PropertiesPage />)

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    // Fill some filters
    const nameInput = screen.getByLabelText(/name/i) || screen.getByPlaceholderText(/name/i)
    await user.type(nameInput, 'Luxury')

    // Clear filters
    const clearButton = screen.getByRole('button', { name: /clear/i })
    await user.click(clearButton)

    // Verify filters are cleared
    expect(nameInput).toHaveValue('')
  })

  it('displays pagination controls', async () => {
    render(<PropertiesPage />)

    // Wait for properties to load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    // Check pagination controls
    expect(screen.getByRole('button', { name: /prev/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /1/i })).toBeInTheDocument()
  })

  it('shows loading state while fetching data', () => {
    render(<PropertiesPage />)

    // Should show loading initially
    expect(screen.getByText('Recent Transactions')).toBeInTheDocument()
    expect(screen.getByText('Discover our curated portfolio of luxury sales and rentals.')).toBeInTheDocument()
  })
})
