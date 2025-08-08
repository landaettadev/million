import type { PropertyDetailDto, PropertyListResponse } from './types'
import type { GetPropertiesParams } from './mock'
// import { getProperties as getPropertiesMock, getPropertyById as getPropertyByIdMock } from './mock'

// Base URL for real API (.NET backend)
const baseUrl = (process.env.NEXT_PUBLIC_API_BASE || process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/$/, '')

export async function getProperties(params?: GetPropertiesParams): Promise<PropertyListResponse> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  const url = new URL(`${baseUrl}/api/properties`)
  if (params?.name) url.searchParams.set('name', params.name)
  if (params?.address) url.searchParams.set('address', params.address)
  if (typeof params?.minPrice === 'number') url.searchParams.set('minPrice', String(params.minPrice))
  if (typeof params?.maxPrice === 'number') url.searchParams.set('maxPrice', String(params.maxPrice))
  if (params?.operationType) url.searchParams.set('operationType', params.operationType)
  if (params?.page) url.searchParams.set('page', String(params.page))
  if (params?.pageSize) url.searchParams.set('pageSize', String(params.pageSize))

  const res = await fetch(url.toString(), { cache: 'no-store' })
  if (!res.ok) throw new Error(`Failed to fetch properties: ${res.status}`)
  const data = (await res.json()) as PropertyListResponse
  return data

  /*
  // FALLBACK TO MOCKS
  return getPropertiesMock(params)
  */
}

export async function getPropertyById(id: string): Promise<PropertyDetailDto | null> {
  if (!baseUrl) throw new Error('NEXT_PUBLIC_API_BASE is not set')

  const res = await fetch(`${baseUrl}/api/properties/${encodeURIComponent(id)}`, { cache: 'no-store' })
  if (res.status === 404) return null
  if (!res.ok) throw new Error(`Failed to fetch property: ${res.status}`)
  const data = (await res.json()) as PropertyDetailDto
  return data

  /*
  // FALLBACK TO MOCKS
  return getPropertyByIdMock(id)
  */
}

export const api = {
  getProperties,
  getProperty: getPropertyById,
}
