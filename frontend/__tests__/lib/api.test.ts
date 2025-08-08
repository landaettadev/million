import { api } from '../../lib/api'

// Mock fetch globally
global.fetch = jest.fn()

const mockFetch = fetch as jest.MockedFunction<typeof fetch>

describe('API', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('getProperties', () => {
    it('should fetch properties with default parameters', async () => {
      const mockResponse = {
        items: [
          {
            id: '1',
            idOwner: 'owner1',
            name: 'Luxury Apartment',
            address: '123 Park Avenue',
            price: 2500000,
            operationType: 'sale',
            image: 'https://example.com/image.jpg'
          }
        ],
        total: 1,
        page: 1,
        pageSize: 20
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse
      } as Response)

      const result = await api.getProperties()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/properties'),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          })
        })
      )

      expect(result).toEqual(mockResponse)
    })

    it('should fetch properties with custom parameters', async () => {
      const mockResponse = {
        items: [],
        total: 0,
        page: 2,
        pageSize: 10
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse
      } as Response)

      const result = await api.getProperties({
        page: 2,
        pageSize: 10,
        name: 'Luxury',
        minPrice: 1000000,
        maxPrice: 5000000,
        operationType: 'sale'
      })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/properties'),
        expect.any(Object)
      )

      expect(result).toEqual(mockResponse)
    })

    it('should handle API errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        json: async () => ({ error: 'Internal server error' })
      } as Response)

      await expect(api.getProperties()).rejects.toThrow()
    })

    it('should handle network errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      await expect(api.getProperties()).rejects.toThrow('Network error')
    })
  })

  describe('getProperty', () => {
    it('should fetch a single property', async () => {
      const mockResponse = {
        id: '1',
        idOwner: 'owner1',
        name: 'Luxury Apartment',
        address: '123 Park Avenue',
        price: 2500000,
        operationType: 'sale',
        images: ['https://example.com/image1.jpg', 'https://example.com/image2.jpg']
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse
      } as Response)

      const result = await api.getProperty('1')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/properties/1'),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          })
        })
      )

      expect(result).toEqual(mockResponse)
    })

    it('should handle property not found', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: async () => ({ error: 'Property not found' })
      } as Response)

      await expect(api.getProperty('nonexistent')).rejects.toThrow()
    })
  })

  describe('error handling', () => {
    it('should handle JSON parsing errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => {
          throw new Error('Invalid JSON')
        }
      } as Response)

      await expect(api.getProperties()).rejects.toThrow('Invalid JSON')
    })

    it('should handle timeout errors', async () => {
      mockFetch.mockImplementationOnce(() => {
        return new Promise((_, reject) => {
          setTimeout(() => reject(new Error('Timeout')), 100)
        })
      })

      await expect(api.getProperties()).rejects.toThrow('Timeout')
    })
  })

  describe('URL construction', () => {
    it('should construct URLs correctly with query parameters', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ items: [], total: 0, page: 1, pageSize: 20 })
      } as Response)

      await api.getProperties({
        name: 'Luxury',
        address: 'Park Avenue',
        minPrice: 1000000,
        maxPrice: 5000000,
        operationType: 'sale',
        page: 1,
        pageSize: 20
      })

      const calledUrl = mockFetch.mock.calls[0][0] as string
      const url = new URL(calledUrl)

      expect(url.searchParams.get('name')).toBe('Luxury')
      expect(url.searchParams.get('address')).toBe('Park Avenue')
      expect(url.searchParams.get('minPrice')).toBe('1000000')
      expect(url.searchParams.get('maxPrice')).toBe('5000000')
      expect(url.searchParams.get('operationType')).toBe('sale')
      expect(url.searchParams.get('page')).toBe('1')
      expect(url.searchParams.get('pageSize')).toBe('20')
    })

    it('should handle undefined parameters', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ items: [], total: 0, page: 1, pageSize: 20 })
      } as Response)

      await api.getProperties({
        name: undefined,
        address: undefined,
        minPrice: undefined,
        maxPrice: undefined,
        operationType: undefined
      })

      const calledUrl = mockFetch.mock.calls[0][0] as string
      const url = new URL(calledUrl)

      expect(url.searchParams.has('name')).toBe(false)
      expect(url.searchParams.has('address')).toBe(false)
      expect(url.searchParams.has('minPrice')).toBe(false)
      expect(url.searchParams.has('maxPrice')).toBe(false)
      expect(url.searchParams.has('operationType')).toBe(false)
    })
  })
})
