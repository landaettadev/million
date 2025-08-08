import { OperationType, PropertyDetailDto, PropertyFilters, PropertyLiteDto, PropertyListResponse } from './types'

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

// Deterministic pseudo-random generator based on seed
const rand = (seed: number) => {
  const x = Math.sin(seed) * 10000
  return x - Math.floor(x)
}

const names = [
  'Serenity Villa',
  'Skyline Penthouse',
  'Coastal Retreat',
  'Metropolitan Loft',
  'Garden Estate',
  'Harbor Residence',
  'Mountain Chalet',
  'Lakeside Manor',
  'Desert Oasis',
  'Urban Haven',
  'Riverview House',
  'Sunset Bungalow',
]

const cities = [
  'Beverly Hills, CA',
  'Upper East Side, NY',
  'South Beach, FL',
  'San Francisco, CA',
  'Chicago, IL',
  'Austin, TX',
  'Seattle, WA',
  'Boston, MA',
  'Denver, CO',
  'Dallas, TX',
  'Las Vegas, NV',
  'San Diego, CA',
]

const toPrice = (seed: number) => {
  // 100k - 5M
  const min = 100_000
  const max = 5_000_000
  return Math.round(min + rand(seed) * (max - min))
}

const toBeds = (seed: number) => 2 + Math.floor(rand(seed) * 5) // 2-6
const toBaths = (seed: number) => 1 + Math.floor(rand(seed + 7) * 4) // 1-4
const toSqft = (seed: number) => 800 + Math.floor(rand(seed + 13) * 9200) // 800-10000

const coverImage = (id: number) => `https://picsum.photos/id/${100 + (id % 800)}/1200/800`
const galleryImages = (id: number) => {
  const count = 3 + Math.floor(rand(id + 23) * 3) // 3-5 images
  return Array.from({ length: count }, (_, i) => `https://picsum.photos/id/${200 + ((id * 3 + i) % 800)}/1600/1000`)
}

// Build dataset
const DATA_LITE: PropertyLiteDto[] = []
const DATA_DETAIL = new Map<string, PropertyDetailDto>()

for (let i = 0; i < 24; i++) {
  const id = (i + 1).toString()
  const name = names[i % names.length]
  const address = `${cities[i % cities.length]}`
  const operationType: OperationType = i % 2 === 0 ? 'sale' : 'rent'
  const price = toPrice(i + 1)
  const beds = toBeds(i + 2)
  const baths = toBaths(i + 3)
  const sqft = toSqft(i + 4)
  const image = coverImage(i)
  const images = galleryImages(i)

  const lite: PropertyLiteDto = {
    id,
    idOwner: `owner-${(i % 8) + 1}`,
    name,
    address,
    price,
    image,
    operationType,
    beds,
    baths,
    sqft,
  }

  const detail: PropertyDetailDto = {
    ...lite,
    images,
    description:
      'Exquisite residence offering refined living with expansive interiors, refined finishes, and exceptional privacy. This is a mock description for demonstration purposes.'
  }

  DATA_LITE.push(lite)
  DATA_DETAIL.set(id, detail)
}

export type GetPropertiesParams = PropertyFilters

export async function getProperties(params: GetPropertiesParams = {}): Promise<PropertyListResponse> {
  await delay(400)

  const {
    name,
    address,
    minPrice,
    maxPrice,
    operationType,
    page = 1,
    pageSize = 12,
  } = params

  let results = [...DATA_LITE]

  if (name) {
    const n = name.toLowerCase()
    results = results.filter((p) => p.name.toLowerCase().includes(n))
  }

  if (address) {
    const a = address.toLowerCase()
    results = results.filter((p) => p.address.toLowerCase().includes(a))
  }

  if (typeof minPrice === 'number') {
    results = results.filter((p) => p.price >= minPrice)
  }

  if (typeof maxPrice === 'number') {
    results = results.filter((p) => p.price <= maxPrice)
  }

  if (operationType) {
    results = results.filter((p) => p.operationType === operationType)
  }

  // Sort by price asc
  results.sort((a, b) => a.price - b.price)

  const total = results.length
  const start = (page - 1) * pageSize
  const end = start + pageSize

  const items = results.slice(start, end)

  return { items, total, page, pageSize }
}

export async function getPropertyById(id: string): Promise<PropertyDetailDto | null> {
  await delay(300)
  return DATA_DETAIL.get(id) ?? null
}
