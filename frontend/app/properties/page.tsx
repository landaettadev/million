'use client'

import { useEffect, useMemo, useState } from 'react'
import { FiltersBar, type FiltersValue } from '@/components/property/FiltersBar'
import { Pagination } from '@/components/property/Pagination'
import { PropertyCarousel } from '@/components/property/PropertyCarousel'
import { api } from '@/lib/api'
import type { PropertyListResponse } from '@/lib/types'

const PAGE_SIZE = 12

const parseInitialFromURL = (): { draft: FiltersValue; page: number } => {
  if (typeof window === 'undefined') return { draft: {}, page: 1 }
  const sp = new URLSearchParams(window.location.search)
  const draft: FiltersValue = {
    name: sp.get('name') || undefined,
    address: sp.get('address') || undefined,
    operationType: (sp.get('operationType') as '' | 'sale' | 'rent') || '',
  }
  const min = sp.get('minPrice')
  const max = sp.get('maxPrice')
  if (min !== null && min !== '') draft.minPrice = Number(min)
  if (max !== null && max !== '') draft.maxPrice = Number(max)
  const page = Number(sp.get('page') || '1')
  return { draft, page: Number.isNaN(page) || page < 1 ? 1 : page }
}

export default function PropertiesPage() {
  const initial = useMemo(parseInitialFromURL, [])

  const [draft, setDraft] = useState<FiltersValue>(initial.draft)
  const [filters, setFilters] = useState<FiltersValue>(initial.draft)
  const [page, setPage] = useState<number>(initial.page)
  const [data, setData] = useState<PropertyListResponse | null>(null)
  const [loading, setLoading] = useState<boolean>(true)

  useEffect(() => {
    let cancelled = false
    const load = async () => {
      setLoading(true)
      try {
        const res = await api.getProperties({
          ...filters,
          page,
          pageSize: PAGE_SIZE,
          operationType: (filters.operationType || undefined) as 'sale' | 'rent' | undefined,
        })
        if (!cancelled) setData(res)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [filters, page])

  useEffect(() => {
    if (typeof window === 'undefined') return
    const sp = new URLSearchParams()
    if (filters.name) sp.set('name', filters.name)
    if (filters.address) sp.set('address', filters.address)
    if (typeof filters.minPrice === 'number') sp.set('minPrice', String(filters.minPrice))
    if (typeof filters.maxPrice === 'number') sp.set('maxPrice', String(filters.maxPrice))
    if (filters.operationType) sp.set('operationType', filters.operationType)
    if (page && page !== 1) sp.set('page', String(page))

    const qs = sp.toString()
    const url = qs ? `?${qs}` : ''
    window.history.pushState({}, '', `${window.location.pathname}${url}`)
  }, [filters, page])

  const onChange = (next: FiltersValue) => setDraft(next)
  const onSubmit = () => {
    setFilters(draft)
    setPage(1)
  }
  const onClear = () => {
    setDraft({})
    setFilters({})
    setPage(1)
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1

  return (
    <div className="min-h-screen bg-white">
      <div className="bg-white">
        <div className="container pt-14 pb-6">
          <h1 className="text-center font-serif uppercase tracking-wide text-4xl md:text-5xl text-black mb-3">
            Recent Transactions
          </h1>
          <p className="text-center text-[#6B7280] max-w-2xl mx-auto">
            Discover our curated portfolio of luxury sales and rentals.
          </p>
        </div>
      </div>

      <FiltersBar value={draft} onChange={onChange} onSubmit={onSubmit} onClear={onClear} />

      <div className="container py-16">
        {loading || !data ? (
          // Skeleton via empty carousel with placeholders
          <PropertyCarousel
            items={Array.from({ length: 4 }).map((_, i) => ({
              id: `sk-${i}`,
              idOwner: 'loading',
              name: 'Loading',
              address: 'Loading',
              price: 0,
              image: '',
              operationType: 'sale',
            }))}
          />
        ) : data.items.length > 0 ? (
          <PropertyCarousel items={data.items} />
        ) : (
          <div className="text-center py-20">
            <h3 className="font-serif text-2xl text-black mb-2">No properties found</h3>
            <p className="text-[#6B7280]">Try adjusting your filters to find more properties.</p>
          </div>
        )}

        {data && data.total > 0 && (
          <Pagination page={page} pageSize={PAGE_SIZE} total={data.total} onChange={(p) => setPage(p)} />
        )}
      </div>
    </div>
  )
}
