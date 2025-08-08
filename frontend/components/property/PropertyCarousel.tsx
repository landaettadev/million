"use client"

import { useRef, useState } from "react"
import { PropertyCard } from "@/components/property/PropertyCard"
import type { PropertyLiteDto } from "@/lib/types"

interface PropertyCarouselProps {
  items: PropertyLiteDto[]
}

export function PropertyCarousel({ items }: PropertyCarouselProps) {
  const scrollerRef = useRef<HTMLDivElement | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [startX, setStartX] = useState(0)
  const [startScrollLeft, setStartScrollLeft] = useState(0)

  const onMouseDown = (e: React.MouseEvent) => {
    if (!scrollerRef.current) return
    setIsDragging(true)
    setStartX(e.clientX)
    setStartScrollLeft(scrollerRef.current.scrollLeft)
  }

  const onMouseMove = (e: React.MouseEvent) => {
    if (!isDragging || !scrollerRef.current) return
    const dx = e.clientX - startX
    scrollerRef.current.scrollLeft = startScrollLeft - dx
  }

  const endDrag = () => setIsDragging(false)

  const onTouchStart = (e: React.TouchEvent) => {
    if (!scrollerRef.current) return
    setIsDragging(true)
    setStartX(e.touches[0].clientX)
    setStartScrollLeft(scrollerRef.current.scrollLeft)
  }

  const onTouchMove = (e: React.TouchEvent) => {
    if (!isDragging || !scrollerRef.current) return
    const dx = e.touches[0].clientX - startX
    scrollerRef.current.scrollLeft = startScrollLeft - dx
  }

  const scrollBy = (delta: number) => {
    scrollerRef.current?.scrollBy({ left: delta, behavior: "smooth" })
  }

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowRight") {
      e.preventDefault()
      scrollBy(400)
    } else if (e.key === "ArrowLeft") {
      e.preventDefault()
      scrollBy(-400)
    }
  }

  return (
    <div className="relative w-full" aria-label="Property carousel" tabIndex={0} onKeyDown={onKeyDown}>
      {/* Left Arrow */}
      <button
        type="button"
        aria-label="Previous property"
        onClick={() => scrollBy(-400)}
        className="hidden md:flex items-center justify-center w-10 h-10 rounded-full bg-white/90 text-black shadow hover:bg-white transition absolute left-3 top-1/2 -translate-y-1/2 z-10"
      >
        ‹
      </button>

      {/* Right Arrow */}
      <button
        type="button"
        aria-label="Next property"
        onClick={() => scrollBy(400)}
        className="hidden md:flex items-center justify-center w-10 h-10 rounded-full bg-white/90 text-black shadow hover:bg-white transition absolute right-3 top-1/2 -translate-y-1/2 z-10"
      >
        ›
      </button>

      <div
        ref={scrollerRef}
        className="flex gap-8 overflow-x-auto scroll-smooth snap-x snap-mandatory pb-4 no-scrollbar cursor-grab active:cursor-grabbing"
        style={{ scrollbarWidth: "none" }}
        onMouseDown={onMouseDown}
        onMouseMove={onMouseMove}
        onMouseLeave={endDrag}
        onMouseUp={endDrag}
        onTouchStart={onTouchStart}
        onTouchMove={onTouchMove}
        onTouchEnd={endDrag}
      >
        {items.map((item) => (
          <div key={item.id} className="snap-start shrink-0 w-[90%] md:w-[70%] lg:w-[50%]">
            <PropertyCard item={item} />
          </div>
        ))}
      </div>

      <style jsx global>{`
        .no-scrollbar { -ms-overflow-style: none; scrollbar-width: none; }
        .no-scrollbar::-webkit-scrollbar { display: none; }
      `}</style>
    </div>
  )
}
