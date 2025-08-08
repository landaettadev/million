export type OperationType = 'sale' | 'rent'

export interface PropertyLiteDto {
  id: string
  idOwner: string
  name: string
  address: string
  price: number
  image?: string
  operationType: OperationType
  beds?: number
  baths?: number
  sqft?: number
}

export interface PropertyDetailDto extends PropertyLiteDto {
  images: string[]
  description?: string
}

// Optional filters and list response helpers
export interface PropertyFilters {
  name?: string
  address?: string
  minPrice?: number
  maxPrice?: number
  operationType?: OperationType
  page?: number
  pageSize?: number
}

export interface PropertyListResponse {
  items: PropertyLiteDto[]
  total: number
  page: number
  pageSize: number
}

// Backward-compatibility aliases (used elsewhere in the project)
export type Property = PropertyLiteDto
export type PropertyDetail = PropertyDetailDto
