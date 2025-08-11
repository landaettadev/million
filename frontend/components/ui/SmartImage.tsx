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
  const [currentSrc, setCurrentSrc] = useState(src)
  const [hasError, setHasError] = useState(false)

  // Generate fallback image if none provided
  const generateFallback = () => {
    if (fallbackSrc) return fallbackSrc
    
    // Only use placeholder if we absolutely have to
    // For now, use a simple placeholder
    return 'https://picsum.photos/800/600?random=1'
  }

  const handleError = () => {
    if (!hasError) {
      setHasError(true)
      console.log('Image failed to load:', src)
      
      // Only use fallback if the image is not from Azure Blob Storage
      if (!src.includes('millionstorageprod.blob.core.windows.net')) {
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
    setCurrentSrc(src)
    setHasError(false)
    
    // Log the image source being used
    if (src.includes('millionstorageprod.blob.core.windows.net')) {
      console.log('Using Azure image:', src)
    } else if (src.includes('picsum.photos')) {
      console.log('Using placeholder image:', src)
    } else {
      console.log('Using other image source:', src)
    }
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
      unoptimized={currentSrc.includes('millionstorageprod.blob.core.windows.net')}
      {...imageProps}
    />
  )
}
