import { render, screen } from '@testing-library/react'
import { PropertyCard } from '../../../components/property/PropertyCard'
import type { PropertyLiteDto } from '../../../lib/types'

const mockProperty: PropertyLiteDto = {
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

describe('PropertyCard Component', () => {
  it('renders property information correctly', () => {
    render(<PropertyCard item={mockProperty} />)
    
    expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    expect(screen.getByText('$2.5M')).toBeInTheDocument()
    // Address is in the alt text of the image, not as separate text
    expect(screen.getByAltText('Luxury Penthouse — 123 Park Avenue, New York')).toBeInTheDocument()
  })

  it('renders property specifications correctly', () => {
    render(<PropertyCard item={mockProperty} />)
    
    expect(screen.getByText(/3 Beds/)).toBeInTheDocument()
    expect(screen.getByText(/2 Baths/)).toBeInTheDocument()
    expect(screen.getByText(/1 Half Bath/)).toBeInTheDocument()
    expect(screen.getByText(/2,500 Sq\. Ft\./)).toBeInTheDocument()
  })

  it('renders property image with correct alt text', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const image = screen.getByAltText('Luxury Penthouse — 123 Park Avenue, New York')
    expect(image).toBeInTheDocument()
    expect(image).toHaveAttribute('src', 'https://picsum.photos/800/600?random=1')
  })

  it('handles missing optional specifications', () => {
    const propertyWithoutSpecs: PropertyLiteDto = {
      ...mockProperty,
      beds: undefined,
      baths: undefined,
      halfBaths: undefined,
      sqft: undefined,
    }

    render(<PropertyCard item={propertyWithoutSpecs} />)
    
    expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    expect(screen.getByText('$2.5M')).toBeInTheDocument()
    // Specs section should not be rendered
    expect(screen.queryByText(/Beds/)).not.toBeInTheDocument()
  })

  it('handles single half bath correctly', () => {
    const propertyWithOneHalfBath: PropertyLiteDto = {
      ...mockProperty,
      halfBaths: 1,
    }

    render(<PropertyCard item={propertyWithOneHalfBath} />)
    
    expect(screen.getByText(/1 Half Bath/)).toBeInTheDocument()
  })

  it('handles multiple half baths correctly', () => {
    const propertyWithMultipleHalfBaths: PropertyLiteDto = {
      ...mockProperty,
      halfBaths: 2,
    }

    render(<PropertyCard item={propertyWithMultipleHalfBaths} />)
    
    expect(screen.getByText(/2 Half Baths/)).toBeInTheDocument()
  })

  it('has correct accessibility attributes', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const article = screen.getByRole('article')
    expect(article).toBeInTheDocument()
    
    const image = screen.getByRole('img')
    expect(image).toHaveAttribute('alt', 'Luxury Penthouse — 123 Park Avenue, New York')
  })

  it('applies group hover classes correctly', () => {
    render(<PropertyCard item={mockProperty} />)
    
    const article = screen.getByRole('article')
    expect(article).toHaveClass('group')
  })
})
