import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { PropertyCard } from '@/components/property/PropertyCard'
import type { PropertyLiteDto } from '@/lib/types'

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

const mockProperty: PropertyLiteDto = {
  id: 'test-id-123',
  idOwner: 'owner-123',
  name: 'Luxury Apartment',
  address: '123 Park Avenue, New York',
  price: 2500000,
  image: 'https://example.com/image.jpg',
  operationType: 'sale',
  beds: 3,
  baths: 2,
  sqft: 2500
}

describe('PropertyCard', () => {
  it('renders property information correctly', () => {
    render(<PropertyCard item={mockProperty} />)
    
    expect(screen.getByText('Luxury Apartment')).toBeInTheDocument()
    expect(screen.getByText('3 Beds')).toBeInTheDocument()
    expect(screen.getByText('2 Baths')).toBeInTheDocument()
    expect(screen.getByText('2,500 Sq. Ft.')).toBeInTheDocument()
    expect(screen.getByText('$2.5M')).toBeInTheDocument()
  })

  it('renders loading skeleton when loading is true', () => {
    render(<PropertyCard item={mockProperty} loading={true} />)
    
    expect(screen.getByLabelText('Loading project name')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading title')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading specs')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading price')).toBeInTheDocument()
  })

  it('displays property image when available', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const image = screen.getByAltText('Luxury Apartment — 123 Park Avenue, New York')
    expect(image).toBeInTheDocument()
    expect(image).toHaveAttribute('src', 'https://example.com/image.jpg')
  })

  it('shows placeholder when no image is available', () => {
    const propertyWithoutImage = { ...mockProperty, image: undefined }
    render(<PropertyCard item={propertyWithoutImage} />)
    
    expect(screen.queryByAltText(/Luxury Apartment/)).not.toBeInTheDocument()
  })

  it('renders project name when available', () => {
    const propertyWithProject = { ...mockProperty, projectName: 'Luxury Collection' }
    render(<PropertyCard item={propertyWithProject} />)
    
    expect(screen.getByText('Luxury Collection')).toBeInTheDocument()
  })

  it('handles half baths correctly', () => {
    const propertyWithHalfBaths = { ...mockProperty, halfBaths: 1 }
    render(<PropertyCard item={propertyWithHalfBaths} />)
    
    expect(screen.getByText('1 Half Bath')).toBeInTheDocument()
  })

  it('handles multiple half baths correctly', () => {
    const propertyWithHalfBaths = { ...mockProperty, halfBaths: 2 }
    render(<PropertyCard item={propertyWithHalfBaths} />)
    
    expect(screen.getByText('2 Half Baths')).toBeInTheDocument()
  })

  it('renders link with correct href', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const link = screen.getByRole('link')
    expect(link).toHaveAttribute('href', '/properties/test-id-123')
  })

  it('has correct accessibility attributes', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const link = screen.getByRole('link')
    expect(link).toHaveAttribute('aria-label', 'View details of Luxury Apartment')
  })

  it('formats price correctly', () => {
    const expensiveProperty = { ...mockProperty, price: 5000000 }
    render(<PropertyCard item={expensiveProperty} />)
    
    expect(screen.getByText('$5.0M')).toBeInTheDocument()
  })

  it('handles properties without optional fields', () => {
    const minimalProperty = {
      id: 'minimal-id',
      idOwner: 'owner-123',
      name: 'Minimal Property',
      address: '456 Simple St',
      price: 1000000,
      operationType: 'rent' as const
    }
    
    render(<PropertyCard item={minimalProperty} />)
    
    expect(screen.getByText('Minimal Property')).toBeInTheDocument()
    expect(screen.getByText('$1.0M')).toBeInTheDocument()
    
    // Should not render specs if not available
    expect(screen.queryByText(/Beds/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Baths/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Sq\. Ft\./)).not.toBeInTheDocument()
  })
})
