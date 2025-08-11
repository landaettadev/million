import type { PropertyDetailDto, PropertyLiteDto, PropertyListResponse } from './types'
import type { GetPropertiesParams } from './mock'
import { ApiError, ApiErrorResponse, handleApiError, TimeoutError } from './errors'
// import { getProperties as getPropertiesMock, getPropertyById as getPropertyByIdMock } from './mock'

// Base URL for real API (.NET backend)
const baseUrl = (process.env.NEXT_PUBLIC_API_BASE || process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/$/, '')

// Map backend OperationType enum to frontend string
function mapOperationType(backendType: number): 'sale' | 'rent' {
  return backendType === 0 ? 'sale' : 'rent'
}

// Map backend property data to frontend format
function mapBackendPropertyToLite(backendProperty: any): PropertyLiteDto {
  return {
    id: backendProperty.Id,
    idOwner: backendProperty.IdOwner,
    name: backendProperty.Name,
    address: backendProperty.Address,
    price: backendProperty.Price,
    image: backendProperty.Image,
    operationType: mapOperationType(backendProperty.OperationType),
    beds: backendProperty.Beds,
    baths: backendProperty.Baths,
    sqft: backendProperty.Sqft
  }
}

// Map backend property detail data to frontend format
function mapBackendPropertyToDetail(backendProperty: any): PropertyDetailDto {
  return {
    id: backendProperty.Id,
    idOwner: backendProperty.IdOwner,
    name: backendProperty.Name,
    address: backendProperty.Address,
    price: backendProperty.Price,
    image: backendProperty.Image,
    operationType: mapOperationType(backendProperty.OperationType),
    beds: backendProperty.Beds,
    baths: backendProperty.Baths,
    sqft: backendProperty.Sqft,
    images: backendProperty.Images || [],
    description: backendProperty.Description
  }
}

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

      const backendData = await res.json()
      const mappedItems = backendData.items.map(mapBackendPropertyToLite)
      const data: PropertyListResponse = {
        items: mappedItems,
        total: backendData.total,
        page: backendData.page,
        pageSize: backendData.pageSize
      }
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

      const backendData = await res.json()
      const data = mapBackendPropertyToDetail(backendData)
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

export async function getFeaturedProperties(limit: number = 6): Promise<PropertyLiteDto[]> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  try {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 30000) // 30 second timeout

    try {
      const res = await fetch(`${baseUrl}/api/properties/featured?limit=${limit}`, { 
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

      const backendData = await res.json()
      const data = backendData.map(mapBackendPropertyToLite)
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
}

// Admin API functions for image management
export async function deletePropertyImage(imageId: string): Promise<void> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  try {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 30000)

    try {
      const res = await fetch(`${baseUrl}/api/admin/images/${imageId}`, {
        method: 'DELETE',
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
}

export async function getPropertyDetail(propertyId: string): Promise<any> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  try {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 30000)

    try {
      const res = await fetch(`${baseUrl}/api/admin/properties/${propertyId}`, {
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

      const data = await res.json()
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
}

export const api = {
  getProperties,
  getProperty: getPropertyById,
  getFeaturedProperties,
  deletePropertyImage,
  getPropertyDetail,
}
