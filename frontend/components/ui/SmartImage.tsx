'use client'

import { useState, useEffect } from 'react'
import Image from 'next/image'

interface SmartImageProps {
  src: string
  alt: string
  fallbackSrc?: string
  className?: string
  fill?: boolean
  sizes?: string
  priority?: boolean
  onError?: () => void
  [key: string]: any
}

export function SmartImage({ 
  src, 
  alt, 
  fallbackSrc, 
  className = '', 
  fill = false,
  sizes,
  priority = false,
  onError,
  ...props 
}: SmartImageProps) {
  const [currentSrc, setCurrentSrc] = useState(src || '/hero-poster.jpg')
  const [hasError, setHasError] = useState(false)

  // Generate fallback image if none provided (avoid external placeholders)
  const generateFallback = () => {
    if (fallbackSrc) return fallbackSrc
    // No external placeholder usage. Keep original src to let browser handle error gracefully.
    return src
  }

  const handleError = () => {
    if (!hasError) {
      setHasError(true)
      console.log('Image failed to load:', src)
      
      // Always use fallback in development or when configured
      const usePlaceholderImages = process.env.NEXT_PUBLIC_USE_PLACEHOLDER_IMAGES === 'true'
      if (usePlaceholderImages || !src.includes('millionstorageprod.blob.core.windows.net')) {
        console.log('Using fallback image for:', src)
        setCurrentSrc(generateFallback())
      } else {
        console.log('Azure image failed, but not using fallback for:', src)
      }
      onError?.()
    }
  }

  // Reset error state when src changes
  useEffect(() => {
    setCurrentSrc(src || '/hero-poster.jpg')
    setHasError(false)
    // Avoid logging placeholder sources; prefer silent behavior
  }, [src])

  // Only pass priority prop if it's true
  const imageProps = {
    ...props,
    ...(priority && { priority: true })
  }

  return (
    <Image
      src={currentSrc}
      alt={alt}
      className={className}
      fill={fill}
      sizes={sizes}
      onError={handleError}
      unoptimized={currentSrc.includes('millionstorageprod.blob.core.windows.net') || currentSrc.includes('127.0.0.1:10000')}
      {...imageProps}
    />
  )
}
