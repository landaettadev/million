'use client'

import Link from 'next/link'
import { useEffect, useMemo, useState } from 'react'
import { ArrowRight, Star, TrendingUp } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { PropertyCard } from '@/components/property/PropertyCard'
import { api } from '@/lib/api'
import type { PropertyLiteDto } from '@/lib/types'

export default function HomePage() {
  const [loading, setLoading] = useState(true)
  const [all, setAll] = useState<PropertyLiteDto[]>([])
  const [reducedMotion, setReducedMotion] = useState(false)

  useEffect(() => {
    // prefers-reduced-motion handling
    if (typeof window !== 'undefined' && 'matchMedia' in window) {
      const mq = window.matchMedia('(prefers-reduced-motion: reduce)')
      setReducedMotion(mq.matches)
      const handler = () => setReducedMotion(mq.matches)
      mq.addEventListener ? mq.addEventListener('change', handler) : mq.addListener(handler)
      return () => {
        mq.removeEventListener ? mq.removeEventListener('change', handler) : mq.removeListener(handler)
      }
    }
  }, [])

  const featured = useMemo(() => {
    return [...all].sort((a, b) => b.price - a.price).slice(0, 3)
  }, [all])

  const latest = useMemo(() => {
    return [...all].slice(0, 6)
  }, [all])

  useEffect(() => {
    let active = true
    const load = async () => {
      setLoading(true)
      try {
        const res = await api.getProperties({ page: 1, pageSize: 24 })
        if (active) setAll(res.items)
      } finally {
        if (active) setLoading(false)
      }
    }
    load()
    return () => {
      active = false
    }
  }, [])

  return (
    <div className="min-h-screen">
      {/* Hero video */}
      <section className="relative h-[100svh] overflow-hidden">
        {!reducedMotion ? (
          <video
            className="absolute inset-0 h-full w-full object-cover"
            autoPlay
            muted
            loop
            playsInline
            preload="metadata"
            poster="/hero-poster.jpg"
            aria-label="Luxury residences video background"
          >
            <source src="/index.mp4" type="video/mp4" />
          </video>
        ) : (
          <img
            src="/hero-poster.jpg"
            alt="Luxury residences poster"
            className="absolute inset-0 h-full w-full object-cover"
          />
        )}
        <div className="absolute inset-0 bg-black/30" />

        <div className="relative z-10 container h-full flex items-center">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center w-full">
            {/* Left block */}
            <div className="text-left md:text-left font-serif uppercase tracking-wide">
              <div className="space-y-2">
                <h1 className="text-5xl md:text-6xl font-light">#1 Team</h1>
                <h2 className="text-4xl md:text-5xl font-light">In The US</h2>
                <p className="text-xl md:text-2xl font-light opacity-90">In New Construction</p>
              </div>

              <div className="mt-8 flex flex-col sm:flex-row gap-4">
                <Link href="/properties" aria-label="Explore properties">
                  <Button variant="primary" size="md" className="px-8">Browse Properties</Button>
                </Link>
                <Link href="/properties" aria-label="List your property">
                  <Button variant="ghost" size="md" className="px-8">List your property</Button>
                </Link>
              </div>
            </div>

            {/* Right block */}
            <div className="text-left md:text-right font-serif">
              <p className="text-3xl md:text-4xl">More Than</p>
              <p className="text-5xl md:text-6xl font-medium">$2.1 Billion</p>
              <p className="text-xl md:text-2xl opacity-90">In Sales</p>
            </div>
          </div>
        </div>

        {/* Brands strip */}
        <div className="absolute bottom-6 left-0 right-0 z-10">
          <div className="container flex flex-wrap items-center justify-center gap-8 text-sm md:text-base text-white/80">
            <span>Bloomberg</span>
            <span>The Wall Street Journal</span>
            <span>The New York Times</span>
            <span>Forbes</span>
          </div>
        </div>
      </section>

      {/* Featured Properties */}
      <section className="border-t border-white/10">
        <div className="container py-16 md:py-20">
          <div className="text-center mb-12">
            <div className="flex items-center justify-center gap-3 mb-3">
              <Star className="h-5 w-5" />
              <h2 className="font-serif tracking-tight leading-tight text-3xl md:text-4xl font-semibold">Featured Properties</h2>
            </div>
            <p className="text-white/60 max-w-2xl mx-auto">
              Handpicked selection of our most exceptional properties, each offering unparalleled luxury and prime locations.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {loading
              ? Array.from({ length: 3 }).map((_, i) => (
                  <PropertyCard
                    key={`fsk-${i}`}
                    item={{ id: String(i), idOwner: 'loading', name: 'Loading', address: 'Loading', price: 0, operationType: 'sale' }}
                    loading
                  />
                ))
              : featured.map((p) => <PropertyCard key={p.id} item={p} />)}
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="border-t border-white/10">
        <div className="container py-14" aria-live="polite">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            <div>
              <div className="text-4xl md:text-5xl font-bold">150+</div>
              <div className="uppercase tracking-wider text-xs mt-2 text-white/60">Premium Properties</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold">$2.5B+</div>
              <div className="uppercase tracking-wider text-xs mt-2 text-white/60">Properties Sold</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold">50+</div>
              <div className="uppercase tracking-wider text-xs mt-2 text-white/60">Prime Locations</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold">25</div>
              <div className="uppercase tracking-wider text-xs mt-2 text-white/60">Years Experience</div>
            </div>
          </div>
        </div>
      </section>

      {/* Latest Listings */}
      <section className="border-t border-white/10">
        <div className="container py-16 md:py-20">
          <div className="flex items-end justify-between mb-10">
            <div>
              <div className="flex items-center gap-3 mb-3">
                <TrendingUp className="h-5 w-5" />
                <h2 className="font-serif tracking-tight leading-tight text-3xl md:text-4xl font-semibold">Latest Listings</h2>
              </div>
              <p className="text-white/60">Recently added properties to our exclusive portfolio.</p>
            </div>
            <Link href="/properties" className="hidden md:block" aria-label="View all properties">
              <Button variant="ghost">View All Properties</Button>
            </Link>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
            {loading
              ? Array.from({ length: 6 }).map((_, i) => (
                  <PropertyCard
                    key={`lsk-${i}`}
                    item={{ id: String(i), idOwner: 'loading', name: 'Loading', address: 'Loading', price: 0, operationType: 'sale' }}
                    loading
                  />
                ))
              : latest.map((p) => <PropertyCard key={p.id} item={p} />)}
          </div>
        </div>
      </section>
    </div>
  )
}
