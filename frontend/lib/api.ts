import type { PropertyDetailDto, PropertyLiteDto, PropertyListResponse } from './types'
import type { GetPropertiesParams } from './mock'
import { ApiError, ApiErrorResponse, handleApiError, TimeoutError } from './errors'
// import { getProperties as getPropertiesMock, getPropertyById as getPropertyByIdMock } from './mock'

// Base URL for real API (.NET backend)
const baseUrl = (process.env.NEXT_PUBLIC_API_BASE || process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/$/, '')

// Helper function to build full image URL
function buildImageUrl(imagePath: string): string {
  if (!imagePath) return ''
  
  // If it's already a full URL, return as is
  if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
    return imagePath
  }
  
  // Use the actual Azure Blob Storage URLs directly
  // The images are publicly accessible at: https://millionstorageprod.blob.core.windows.net/property-images/{imagePath}
  const storageAccountName = (process.env.NEXT_PUBLIC_IMAGE_ACCOUNT || 'millionstorageprod')
  const containerName = (process.env.NEXT_PUBLIC_IMAGE_CONTAINER || 'property-images')
  
  // Return the full Azure Blob Storage URL
  const fullUrl = `https://${storageAccountName}.blob.core.windows.net/${containerName}/${imagePath}`
  
  return fullUrl
}

// Map backend OperationType enum to frontend string
function mapOperationType(backendType: number): 'sale' | 'rent' {
  return backendType === 0 ? 'sale' : 'rent'
}

// Map backend property data to frontend format
function mapBackendPropertyToLite(backendProperty: any): PropertyLiteDto {
  const id = backendProperty.id ?? backendProperty.Id
  const idOwner = backendProperty.idOwner ?? backendProperty.IdOwner
  const name = backendProperty.name ?? backendProperty.Name
  const address = backendProperty.address ?? backendProperty.Address
  const price = backendProperty.price ?? backendProperty.Price
  const image = backendProperty.image ?? backendProperty.Image
  const operationTypeRaw = backendProperty.operationType ?? backendProperty.OperationType
  const beds = backendProperty.beds ?? backendProperty.Beds
  const baths = backendProperty.baths ?? backendProperty.Baths
  const sqft = backendProperty.sqft ?? backendProperty.Sqft

  // OperationType can come as number (0/1) or string ('sale'/'rent')
  const operationType: 'sale' | 'rent' = typeof operationTypeRaw === 'number'
    ? mapOperationType(operationTypeRaw)
    : (String(operationTypeRaw).toLowerCase() === 'rent' ? 'rent' : 'sale')

  return {
    id,
    idOwner,
    name,
    address,
    price,
    image: buildImageUrl(image),
    operationType,
    beds,
    baths,
    sqft
  }
}

// Map backend property detail data to frontend format
function mapBackendPropertyToDetail(backendProperty: any): PropertyDetailDto {
  const id = backendProperty.id ?? backendProperty.Id
  const idOwner = backendProperty.idOwner ?? backendProperty.IdOwner
  const name = backendProperty.name ?? backendProperty.Name
  const address = backendProperty.address ?? backendProperty.Address
  const price = backendProperty.price ?? backendProperty.Price
  const image = backendProperty.image ?? backendProperty.Image
  const operationTypeRaw = backendProperty.operationType ?? backendProperty.OperationType
  const beds = backendProperty.beds ?? backendProperty.Beds
  const baths = backendProperty.baths ?? backendProperty.Baths
  const sqft = backendProperty.sqft ?? backendProperty.Sqft
  const images = (backendProperty.images ?? backendProperty.Images ?? []).map(buildImageUrl)
  const description = backendProperty.description ?? backendProperty.Description

  const operationType: 'sale' | 'rent' = typeof operationTypeRaw === 'number'
    ? mapOperationType(operationTypeRaw)
    : (String(operationTypeRaw).toLowerCase() === 'rent' ? 'rent' : 'sale')

  return {
    id,
    idOwner,
    name,
    address,
    price,
    image: buildImageUrl(image),
    operationType,
    beds,
    baths,
    sqft,
    images,
    description
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

export async function getPropertyVideoUrl(id: string): Promise<string | null> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')
  try {
    const res = await fetch(`${baseUrl}/api/properties/${encodeURIComponent(id)}/video`, { cache: 'no-store' })
    if (!res.ok) return null
    const data = await res.json().catch(() => null) as { id: string; url?: string | null } | null
    const url = data?.url || null
    return url && url.endsWith('.mp4') ? url : null
  } catch {
    return null
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
  getPropertyVideoUrl,
}
