'use client'

import { useState } from 'react'
import { SmartImage } from '../ui/SmartImage'

interface GalleryProps {
  images: string[]
  videos?: string[]
}

export function Gallery({ images, videos = [] }: GalleryProps) {
  const [index, setIndex] = useState(0)

  const videoItems = videos.filter(v => v && v.endsWith('.mp4'))
  const items: { type: 'image' | 'video'; src: string }[] = [
    ...images.map(src => ({ type: 'image' as const, src })),
    ...videoItems.map(src => ({ type: 'video' as const, src })),
  ]

  if (items.length === 0) return null

  const safeIndex = Math.max(0, Math.min(index, items.length - 1))

  return (
    <div className="space-y-3" aria-live="polite">
      {/* Main media with stable ratio to avoid CLS */}
      <div className="relative aspect-[16/10] overflow-hidden rounded-2xl">
        {items[safeIndex].type === 'image' ? (
          <SmartImage
            key={items[safeIndex].src}
            src={items[safeIndex].src}
            alt={`Property media ${safeIndex + 1} of ${items.length}`}
            fill
            className="object-cover smooth"
            sizes="(max-width: 768px) 100vw, 50vw"
            priority
          />
        ) : (
          <video
            key={items[safeIndex].src}
            className="absolute inset-0 w-full h-full object-cover rounded-2xl bg-black"
            src={items[safeIndex].src}
            controls
            playsInline
            preload="metadata"
          />
        )}
      </div>

      {/* Thumbnails */}
      {items.length > 1 && (
        <div className="grid grid-cols-5 gap-2">
          {items.map((m, i) => (
            <button
              key={m.src + i}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`Show media ${i + 1}`}
              className={`relative aspect-[16/10] overflow-hidden rounded-lg border smooth ${
                i === safeIndex ? 'border-white' : 'border-white/20 hover:border-white/40'
              }`}
            >
              {m.type === 'image' ? (
                <SmartImage 
                  src={m.src} 
                  alt={`Thumbnail ${i + 1}`} 
                  fill 
                  className="object-cover" 
                  sizes="(max-width: 768px) 20vw, 10vw"
                />
              ) : (
                <div className="absolute inset-0 bg-black/60 text-white flex items-center justify-center text-xs">
                  Video
                </div>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
