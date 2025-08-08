import type { PropertyDetailDto, PropertyListResponse } from './types'
import type { GetPropertiesParams } from './mock'
import { ApiError, ApiErrorResponse, handleApiError, TimeoutError } from './errors'
// import { getProperties as getPropertiesMock, getPropertyById as getPropertyByIdMock } from './mock'

// Base URL for real API (.NET backend)
const baseUrl = (process.env.NEXT_PUBLIC_API_BASE || process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/$/, '')

export async function getProperties(params?: GetPropertiesParams): Promise<PropertyListResponse> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  try {
    const url = new URL(`${baseUrl}/api/properties`)
    if (params?.name) url.searchParams.set('name', params.name)
    if (params?.address) url.searchParams.set('address', params.address)
    if (typeof params?.minPrice === 'number') url.searchParams.set('minPrice', String(params.minPrice))
    if (typeof params?.maxPrice === 'number') url.searchParams.set('maxPrice', String(params.maxPrice))
    if (params?.operationType) url.searchParams.set('operationType', params.operationType)
    if (params?.page) url.searchParams.set('page', String(params.page))
    if (params?.pageSize) url.searchParams.set('pageSize', String(params.pageSize))

    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 30000) // 30 second timeout

    try {
      const res = await fetch(url.toString(), { 
        cache: 'no-store',
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
        }
      })

      clearTimeout(timeoutId)

      if (!res.ok) {
        const errorData = await res.json().catch(() => null) as ApiErrorResponse | null
        if (errorData) {
          throw new ApiError(errorData)
        }
        throw new Error(`HTTP ${res.status}: ${res.statusText}`)
      }

      const data = (await res.json()) as PropertyListResponse
      return data
    } catch (error) {
      clearTimeout(timeoutId)
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new TimeoutError()
      }
      throw error
    }
  } catch (error) {
    handleApiError(error)
  }

  /*
  // FALLBACK TO MOCKS
  return getPropertiesMock(params)
  */
}

export async function getPropertyById(id: string): Promise<PropertyDetailDto | null> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  try {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 30000) // 30 second timeout

    try {
      const res = await fetch(`${baseUrl}/api/properties/${encodeURIComponent(id)}`, { 
        cache: 'no-store',
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
        }
      })

      clearTimeout(timeoutId)

      if (res.status === 404) return null

      if (!res.ok) {
        const errorData = await res.json().catch(() => null) as ApiErrorResponse | null
        if (errorData) {
          throw new ApiError(errorData)
        }
        throw new Error(`HTTP ${res.status}: ${res.statusText}`)
      }

      const data = (await res.json()) as PropertyDetailDto
      return data
    } catch (error) {
      clearTimeout(timeoutId)
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new TimeoutError()
      }
      throw error
    }
  } catch (error) {
    handleApiError(error)
  }

  /*
  // FALLBACK TO MOCKS
  return getPropertyByIdMock(id)
  */
}

export const api = {
  getProperties,
  getProperty: getPropertyById,
}
