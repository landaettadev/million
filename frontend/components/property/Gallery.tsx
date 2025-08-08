'use client'

import { useState } from 'react'
import Image from 'next/image'

interface GalleryProps {
  images: string[]
}

export function Gallery({ images }: GalleryProps) {
  const [index, setIndex] = useState(0)

  if (!images || images.length === 0) return null

  const safeIndex = Math.max(0, Math.min(index, images.length - 1))

  return (
    <div className="space-y-3" aria-live="polite">
      {/* Main image with stable ratio to avoid CLS */}
      <div className="relative aspect-[16/10] overflow-hidden rounded-2xl">
        <Image
          key={images[safeIndex]}
          src={images[safeIndex]}
          alt={`Property image ${safeIndex + 1} of ${images.length}`}
          fill
          className="object-cover smooth"
          sizes="(max-width: 768px) 100vw, 50vw"
          priority
        />
      </div>

      {/* Thumbnails */}
      {images.length > 1 && (
        <div className="grid grid-cols-5 gap-2">
          {images.map((src, i) => (
            <button
              key={src + i}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`Show image ${i + 1}`}
              className={`relative aspect-[16/10] overflow-hidden rounded-lg border smooth ${
                i === safeIndex ? 'border-white' : 'border-white/20 hover:border-white/40'
              }`}
            >
              <Image src={src} alt={`Thumbnail ${i + 1}`} fill className="object-cover" sizes="(max-width: 768px) 20vw, 10vw" />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
