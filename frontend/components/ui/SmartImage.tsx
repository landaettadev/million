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
    
    // Use environment configuration for fallback images
    const usePlaceholderImages = process.env.NEXT_PUBLIC_USE_PLACEHOLDER_IMAGES === 'true'
    const imageBaseUrl = process.env.NEXT_PUBLIC_IMAGE_BASE_URL || 'https://picsum.photos'
    
    if (usePlaceholderImages) {
      return `${imageBaseUrl}/800/600?random=${Math.abs(src.charCodeAt(0)) % 10}`
    }
    
    // Default fallback
    return `${imageBaseUrl}/800/600?random=1`
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
    setCurrentSrc(src)
    setHasError(false)
    
    // Log the image source being used
    const usePlaceholderImages = process.env.NEXT_PUBLIC_USE_PLACEHOLDER_IMAGES === 'true'
    if (usePlaceholderImages) {
      console.log('Using placeholder image (development mode):', src)
    } else if (src.includes('millionstorageprod.blob.core.windows.net')) {
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
      unoptimized={currentSrc.includes('millionstorageprod.blob.core.windows.net') || currentSrc.includes('picsum.photos')}
      {...imageProps}
    />
  )
}
