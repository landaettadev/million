import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import HomePage from '../../app/page'
import { api } from '../../lib/api'

// Mock the API
jest.mock('../../lib/api', () => ({
  api: {
    getProperties: jest.fn()
  }
}))

// Mock Next.js Link component
jest.mock('next/link', () => {
  return ({ children, href, ...props }: any) => {
    return <a href={href} {...props}>{children}</a>
  }
})

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: any) => {
    return <img src={src} alt={alt} {...props} />
  },
}))

const mockProperties = [
  {
    id: '1',
    idOwner: 'owner1',
    name: 'Luxury Apartment',
    address: '123 Park Avenue, New York',
    price: 2500000,
    image: 'https://example.com/image1.jpg',
    operationType: 'sale' as const,
    beds: 3,
    baths: 2,
    sqft: 2500
  },
  {
    id: '2',
    idOwner: 'owner2',
    name: 'Modern Penthouse',
    address: '456 5th Avenue, New York',
    price: 3500000,
    image: 'https://example.com/image2.jpg',
    operationType: 'sale' as const,
    beds: 4,
    baths: 3,
    sqft: 3200
  },
  {
    id: '3',
    idOwner: 'owner3',
    name: 'Cozy Studio',
    address: '789 Broadway, New York',
    price: 1500000,
    image: 'https://example.com/image3.jpg',
    operationType: 'rent' as const,
    beds: 1,
    baths: 1,
    sqft: 800
  }
]

describe('HomePage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    ;(api.getProperties as jest.Mock).mockResolvedValue({
      items: mockProperties,
      total: mockProperties.length,
      page: 1,
      pageSize: 24
    })
  })

  it('renders hero section with title', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      expect(screen.getByText('#1 Team')).toBeInTheDocument()
      expect(screen.getByText('In The US')).toBeInTheDocument()
      expect(screen.getByText('In New Construction')).toBeInTheDocument()
    })
  })

  it('renders navigation buttons', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      expect(screen.getByRole('link', { name: /Browse Properties/ })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: /List your property/ })).toBeInTheDocument()
    })
  })

  it('loads and displays properties', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      expect(api.getProperties).toHaveBeenCalledWith({ page: 1, pageSize: 24 })
    })
    
    await waitFor(() => {
      expect(screen.getByText('Luxury Apartment')).toBeInTheDocument()
      expect(screen.getByText('Modern Penthouse')).toBeInTheDocument()
      expect(screen.getByText('Cozy Studio')).toBeInTheDocument()
    })
  })

  it('displays featured properties (highest priced)', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      // Should show the most expensive properties first
      const propertyCards = screen.getAllByText(/Luxury Apartment|Modern Penthouse|Cozy Studio/)
      expect(propertyCards).toHaveLength(3)
    })
  })

  it('shows loading state initially', () => {
    ;(api.getProperties as jest.Mock).mockImplementation(() => new Promise(() => {}))
    
    render(<HomePage />)
    
    // Should show loading skeletons
    expect(screen.getAllByLabelText(/Loading/)).toHaveLength(3)
  })

  it('handles API errors gracefully', async () => {
    ;(api.getProperties as jest.Mock).mockRejectedValue(new Error('API Error'))
    
    render(<HomePage />)
    
    await waitFor(() => {
      expect(api.getProperties).toHaveBeenCalled()
    })
    
    // Should not crash and should still show the page structure
    expect(screen.getByText('#1 Team')).toBeInTheDocument()
  })

  it('displays property prices correctly', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      expect(screen.getByText('$2.5M')).toBeInTheDocument()
      expect(screen.getByText('$3.5M')).toBeInTheDocument()
      expect(screen.getByText('$1.5M')).toBeInTheDocument()
    })
  })

  it('renders property images', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      const images = screen.getAllByAltText(/Luxury Apartment|Modern Penthouse|Cozy Studio/)
      expect(images).toHaveLength(3)
    })
  })

  it('has correct accessibility attributes', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      const browseLink = screen.getByRole('link', { name: /Browse Properties/ })
      const listLink = screen.getByRole('link', { name: /List your property/ })
      expect(browseLink).toHaveAttribute('aria-label', 'Explore properties')
      expect(listLink).toHaveAttribute('aria-label', 'List your property')
    })
  })

  it('handles empty property list', async () => {
    ;(api.getProperties as jest.Mock).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      pageSize: 24
    })
    
    render(<HomePage />)
    
    await waitFor(() => {
      expect(api.getProperties).toHaveBeenCalled()
    })
    
    // Should still render the page structure
    expect(screen.getByText('#1 Team')).toBeInTheDocument()
  })

  it('sorts featured properties by price (highest first)', async () => {
    render(<HomePage />)
    
    await waitFor(() => {
      const propertyCards = screen.getAllByText(/Luxury Apartment|Modern Penthouse|Cozy Studio/)
      // The order should be: Modern Penthouse ($3.5M), Luxury Apartment ($2.5M), Cozy Studio ($1.5M)
      expect(propertyCards[0]).toHaveTextContent('Modern Penthouse')
      expect(propertyCards[1]).toHaveTextContent('Luxury Apartment')
      expect(propertyCards[2]).toHaveTextContent('Cozy Studio')
    })
  })
})
