import { getProperties, getPropertyById } from '../../lib/api'
import { server } from '../setup/server'
import { http, HttpResponse } from 'msw'

describe('API Functions', () => {
  describe('getProperties', () => {
    it('fetches properties successfully', async () => {
      const result = await getProperties()
      
      expect(result.items).toHaveLength(2)
      expect(result.page).toBe(1)
      expect(result.pageSize).toBe(20)
      expect(result.total).toBe(2)
      expect(result.items[0]).toMatchObject({
        id: '507f1f77bcf86cd799439011',
        name: 'Luxury Penthouse',
        address: '123 Park Avenue, New York',
        price: 2500000,
        operationType: 'sale',
      })
    })

    it('filters properties by name', async () => {
      const result = await getProperties({ name: 'Luxury' })
      
      expect(result.items).toHaveLength(1)
      expect(result.items[0].name).toBe('Luxury Penthouse')
    })

    it('filters properties by address', async () => {
      const result = await getProperties({ address: '5th Avenue' })
      
      expect(result.items).toHaveLength(1)
      expect(result.items[0].address).toBe('456 5th Avenue, New York')
    })

    it('filters properties by price range', async () => {
      const result = await getProperties({ minPrice: 2000000, maxPrice: 3000000 })
      
      expect(result.items).toHaveLength(1)
      expect(result.items[0].price).toBe(2500000)
    })

    it('filters properties by operation type', async () => {
      const result = await getProperties({ operationType: 'rent' })
      
      expect(result.items).toHaveLength(1)
      expect(result.items[0].operationType).toBe('rent')
    })

    it('handles pagination correctly', async () => {
      const result = await getProperties({ page: 2, pageSize: 1 })
      
      expect(result.page).toBe(2)
      expect(result.pageSize).toBe(1)
      expect(result.items).toHaveLength(1)
    })

    it('throws ApiError on server error', async () => {
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

      await expect(getProperties()).rejects.toThrow('Internal server error')
    })

    it('throws TimeoutError on request timeout', async () => {
      server.use(
        http.get('http://localhost:5244/api/properties', async () => {
          // Simulate a timeout by delaying longer than the 30s timeout
          await new Promise(resolve => setTimeout(resolve, 35000))
          return HttpResponse.json({ items: [], page: 1, pageSize: 20, total: 0 })
        })
      )

      await expect(getProperties()).rejects.toThrow('Request timed out')
    }, 35000) // Increase test timeout

    it('throws NetworkError on network failure', async () => {
      server.use(
        http.get('http://localhost:5244/api/properties', () => {
          return HttpResponse.error()
        })
      )

      await expect(getProperties()).rejects.toThrow()
    })
  })

  describe('getPropertyById', () => {
    it('fetches property by ID successfully', async () => {
      const result = await getPropertyById('507f1f77bcf86cd799439011')
      
      expect(result).toMatchObject({
        id: '507f1f77bcf86cd799439011',
        name: 'Luxury Penthouse',
        address: '123 Park Avenue, New York',
        price: 2500000,
        operationType: 'sale',
        images: expect.arrayContaining([
          'https://picsum.photos/800/600?random=1',
          'https://picsum.photos/800/600?random=2',
          'https://picsum.photos/800/600?random=3',
        ]),
        description: 'Beautiful luxury penthouse with stunning city views.',
      })
    })

    it('returns null for non-existent property', async () => {
      const result = await getPropertyById('nonexistent')
      expect(result).toBeNull()
    })

    it('throws ApiError for invalid ID format', async () => {
      await expect(getPropertyById('invalid')).rejects.toThrow('Invalid property ID format')
    })

    it('throws ApiError on server error', async () => {
      server.use(
        http.get('http://localhost:5244/api/properties/:id', () => {
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

      await expect(getPropertyById('507f1f77bcf86cd799439011')).rejects.toThrow('Internal server error')
    })

    it('throws TimeoutError on request timeout', async () => {
      server.use(
        http.get('http://localhost:5244/api/properties/:id', async () => {
          await new Promise(resolve => setTimeout(resolve, 35000))
          return HttpResponse.json({})
        })
      )

      await expect(getPropertyById('507f1f77bcf86cd799439011')).rejects.toThrow('Request timed out')
    }, 35000)
  })

  describe('Environment configuration', () => {
    it('throws error when API base URL is not set', async () => {
      const originalEnv = process.env.NEXT_PUBLIC_API_BASE
      delete process.env.NEXT_PUBLIC_API_BASE

      await expect(getProperties()).rejects.toThrow('NEXT_PUBLIC_API_BASE is not set')

      process.env.NEXT_PUBLIC_API_BASE = originalEnv
    })
  })
})
