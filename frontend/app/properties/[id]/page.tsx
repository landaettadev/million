'use client'

import { useEffect, useState } from 'react'
import Image from 'next/image'
import { useRouter, useParams } from 'next/navigation'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { Skeleton } from '@/components/ui/Skeleton'
import { ErrorBoundary } from '@/components/ui/ErrorBoundary'
import { ErrorMessage } from '@/components/ui/ErrorMessage'
import { Gallery } from '@/components/property/Gallery'
import { formatPrice } from '@/lib/format'
import { getPropertyById } from '@/lib/api'
import type { PropertyDetailDto } from '@/lib/types'

export default function PropertyDetailPage() {
  const router = useRouter()
  const params = useParams<{ id: string }>()
  const id = Array.isArray(params?.id) ? params.id[0] : params?.id

  const [data, setData] = useState<PropertyDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown | null>(null)

  const loadProperty = async () => {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      const res = await getPropertyById(id)
      setData(res)
    } catch (err) {
      setError(err)
      console.error('Failed to load property:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadProperty()
  }, [id])

  const BackButton = (
    <div className="container pt-6">
      <Button variant="ghost" onClick={() => router.back()} aria-label="Go back">
        Back
      </Button>
    </div>
  )

  if (error) {
    return (
      <ErrorBoundary>
        <div className="min-h-screen">
          {BackButton}
          <div className="container py-24">
            <ErrorMessage 
              error={error} 
              onRetry={loadProperty}
              className="mx-auto max-w-lg"
            />
          </div>
        </div>
      </ErrorBoundary>
    )
  }

  if (loading) {
    return (
      <div className="min-h-screen">
        {BackButton}
        <div className="container py-8 grid md:grid-cols-2 gap-8">
          <Skeleton className="w-full aspect-[16/10] rounded-2xl" aria-label="Loading image" />
          <div className="space-y-4">
            <Skeleton className="h-10 w-3/4" aria-label="Loading title" />
            <Skeleton className="h-5 w-1/2" aria-label="Loading address" />
            <Skeleton className="h-6 w-24" aria-label="Loading badge" />
            <Skeleton className="h-10 w-40" aria-label="Loading price" />
            <Skeleton className="h-5 w-1/3" aria-label="Loading specs" />
            <Skeleton className="h-11 w-44" aria-label="Loading cta" />
          </div>
        </div>
      </div>
    )
  }

  if (!data) {
    return (
      <div className="min-h-screen">
        {BackButton}
        <div className="container py-24 text-center">
          <h1 className="font-serif text-3xl font-semibold mb-4">Not found</h1>
          <p className="text-gray-600 mb-8">The property does not exist or is no longer available.</p>
          <Button variant="primary" onClick={() => router.push('/properties')} aria-label="Back to list">
            Back to list
          </Button>
        </div>
      </div>
    )
  }

  const hasMultiple = data.images && data.images.length > 1

  return (
    <ErrorBoundary>
      <div className="min-h-screen">
      {BackButton}

      <div className="container py-8 grid md:grid-cols-2 gap-8">
        {/* Left: Gallery or single image */}
        <div>
          {hasMultiple ? (
            <Gallery images={data.images} />
          ) : (
            <div className="relative aspect-[16/10] rounded-2xl overflow-hidden">
              <Image
                src={data.images?.[0] || data.image || 'https://picsum.photos/1600/1000'}
                alt={data.name}
                fill
                className="object-cover"
                sizes="(max-width: 768px) 100vw, 50vw"
              />
            </div>
          )}
        </div>

        {/* Right: Info */}
        <div className="space-y-5">
          <div>
            <h1 className="font-serif text-3xl md:text-4xl font-semibold">{data.name}</h1>
            <p className="text-white/60 mt-1">{data.address}</p>
          </div>

          <div className="flex items-center gap-3">
            <Badge kind={data.operationType === 'sale' ? 'sale' : 'rent'}>
              {data.operationType === 'sale' ? 'Sale' : 'Rent'}
            </Badge>
            <span className="font-semibold text-3xl">{formatPrice(data.price)}</span>
          </div>

          <div className="text-white/70 text-sm">
            {(data.beds || data.baths || data.sqft) && (
              <p>
                {data.beds ? `${data.beds} Beds` : ''}
                {data.beds && data.baths ? ' · ' : ''}
                {data.baths ? `${data.baths} Baths` : ''}
                {(data.beds || data.baths) && data.sqft ? ' · ' : ''}
                {data.sqft ? `${data.sqft.toLocaleString()} SqFt` : ''}
              </p>
            )}
            {data.idOwner && <p className="mt-1">Owner: {data.idOwner}</p>}
          </div>

          {data.description && (
            <p className="text-white/80 leading-relaxed">{data.description}</p>
          )}

          <div className="flex flex-col sm:flex-row gap-3 pt-2">
            <Button variant="primary" aria-label="Schedule presentation">Schedule presentation</Button>
            <Button variant="ghost" aria-label="Contact agent">Contact agent</Button>
          </div>
        </div>
      </div>
    </div>
    </ErrorBoundary>
  )
}

