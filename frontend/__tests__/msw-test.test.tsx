import { server } from './setup/server'
import { http, HttpResponse } from 'msw'

describe('MSW Test', () => {
  beforeAll(() => {
    server.listen({ onUnhandledRequest: 'error' })
  })
  
  afterEach(() => {
    server.resetHandlers()
  })
  
  afterAll(() => {
    server.close()
  })

  it('should intercept health endpoint', async () => {
    const response = await fetch('http://localhost:5244/health')
    const data = await response.json()
    
    expect(response.status).toBe(200)
    expect(data.status).toBe('ok')
  })

  it('should intercept properties endpoint', async () => {
    const response = await fetch('http://localhost:5244/api/properties')
    const data = await response.json()
    
    expect(response.status).toBe(200)
    expect(data.items).toBeDefined()
    expect(data.items.length).toBeGreaterThan(0)
    expect(data.items[0].name).toBe('Luxury Penthouse')
  })

  it('should intercept property by ID endpoint', async () => {
    const response = await fetch('http://localhost:5244/api/properties/507f1f77bcf86cd799439011')
    const data = await response.json()
    
    expect(response.status).toBe(200)
    expect(data.name).toBe('Luxury Penthouse')
    expect(data.address).toBe('123 Park Avenue, New York')
  })
})
