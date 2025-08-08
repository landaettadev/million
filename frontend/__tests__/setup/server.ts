import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import type { PropertyListResponse, PropertyDetailDto } from '../../lib/types'

// Mock data
const mockProperties: PropertyListResponse = {
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
    },
    {
      id: '507f1f77bcf86cd799439013',
      idOwner: '507f1f77bcf86cd799439014',
      name: 'Modern Apartment',
      address: '456 5th Avenue, New York',
      price: 1800000,
      image: 'https://picsum.photos/800/600?random=2',
      operationType: 'rent',
      beds: 2,
      baths: 2,
      halfBaths: 0,
      sqft: 1800,
    },
  ],
  page: 1,
  pageSize: 20,
  total: 2,
}

const mockPropertyDetail: PropertyDetailDto = {
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
  images: [
    'https://picsum.photos/800/600?random=1',
    'https://picsum.photos/800/600?random=2',
    'https://picsum.photos/800/600?random=3',
  ],
  description: 'Beautiful luxury penthouse with stunning city views.',
}

// Define request handlers
export const handlers = [
  // Get properties list
  http.get('http://localhost:5244/api/properties', ({ request }) => {
    const url = new URL(request.url)
    const name = url.searchParams.get('name')
    const address = url.searchParams.get('address')
    const minPrice = url.searchParams.get('minPrice')
    const maxPrice = url.searchParams.get('maxPrice')
    const operationType = url.searchParams.get('operationType')
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

    // Simulate filtering
    let filteredItems = [...mockProperties.items]

    if (name) {
      filteredItems = filteredItems.filter(item =>
        item.name.toLowerCase().includes(name.toLowerCase())
      )
    }

    if (address) {
      filteredItems = filteredItems.filter(item =>
        item.address.toLowerCase().includes(address.toLowerCase())
      )
    }

    if (minPrice) {
      filteredItems = filteredItems.filter(item => item.price >= parseInt(minPrice))
    }

    if (maxPrice) {
      filteredItems = filteredItems.filter(item => item.price <= parseInt(maxPrice))
    }

    if (operationType && operationType !== '') {
      filteredItems = filteredItems.filter(item => item.operationType === operationType)
    }

    // Simulate pagination
    const startIndex = (page - 1) * pageSize
    const endIndex = startIndex + pageSize
    const paginatedItems = filteredItems.slice(startIndex, endIndex)

    return HttpResponse.json({
      items: paginatedItems,
      page,
      pageSize,
      total: filteredItems.length,
    })
  }),

  // Get property by ID
  http.get('http://localhost:5244/api/properties/:id', ({ params }) => {
    const { id } = params

    if (id === '507f1f77bcf86cd799439011') {
      return HttpResponse.json(mockPropertyDetail)
    }

    if (id === 'nonexistent') {
      return new HttpResponse(null, { status: 404 })
    }

    if (id === 'invalid') {
      return HttpResponse.json(
        {
          traceId: 'test-trace-id',
          error: 'Invalid property ID format',
          details: ['The provided ID is not a valid ObjectId format'],
          statusCode: 400,
          timestamp: new Date().toISOString(),
        },
        { status: 400 }
      )
    }

    return new HttpResponse(null, { status: 404 })
  }),

  // Health check
  http.get('http://localhost:5244/health', () => {
    return HttpResponse.json({ status: 'ok' })
  }),
]

// Setup MSW server
export const server = setupServer(...handlers)
