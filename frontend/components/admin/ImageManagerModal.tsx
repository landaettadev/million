'use client'

import React, { useState, useEffect } from 'react'
import Image from 'next/image'
import { Modal } from '../ui/Modal'
import { api } from '../../lib/api'

interface PropertyImage {
  id: string
  propertyId: string
  blobName: string
  imageUrl: string
  enabled: boolean
  order: number
  fileName: string
  fileSize: number
  contentType: string
  createdAt: string
}

interface PropertyDetail {
  id: string
  name: string
  images: PropertyImage[]
}

interface ImageManagerModalProps {
  isOpen: boolean
  onClose: () => void
  propertyId: string
  propertyName: string
  onImageDeleted?: () => void
}

export function ImageManagerModal({
  isOpen,
  onClose,
  propertyId,
  propertyName,
  onImageDeleted
}: ImageManagerModalProps) {
  const [property, setProperty] = useState<PropertyDetail | null>(null)
  const [loading, setLoading] = useState(false)
  const [deletingImageId, setDeletingImageId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Load property detail with images
  useEffect(() => {
    if (isOpen && propertyId) {
      loadPropertyImages()
    }
  }, [isOpen, propertyId])

  const loadPropertyImages = async () => {
    setLoading(true)
    setError(null)
    try {
      const propertyDetail = await api.getPropertyDetail(propertyId)
      if (propertyDetail) {
        setProperty(propertyDetail)
      } else {
        setError('Property not found')
      }
    } catch (err) {
      console.error('Error loading property images:', err)
      setError('Failed to load images')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteImage = async (imageId: string, fileName: string) => {
    if (!confirm(`¿Estás seguro de que quieres eliminar la imagen "${fileName}"?`)) {
      return
    }

    setDeletingImageId(imageId)
    try {
      await api.deletePropertyImage(imageId)
      
      // Update local state to remove the deleted image
      setProperty(prev => prev ? {
        ...prev,
        images: prev.images.filter(img => img.id !== imageId)
      } : null)

      // Call callback to refresh parent component
      onImageDeleted?.()
    } catch (err) {
      console.error('Error deleting image:', err)
      alert('Error al eliminar la imagen. Inténtalo de nuevo.')
    } finally {
      setDeletingImageId(null)
    }
  }

  const handleClose = () => {
    setProperty(null)
    setError(null)
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={`Gestionar Imágenes - ${propertyName}`}
      maxWidth="2xl"
    >
      <div className="p-6">
        {loading && (
          <div className="flex items-center justify-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <span className="ml-2 text-gray-600">Cargando imágenes...</span>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 rounded-md p-4 mb-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-red-800">Error</h3>
                <div className="mt-2 text-sm text-red-700">{error}</div>
              </div>
            </div>
          </div>
        )}

        {property && property.images.length === 0 && (
          <div className="text-center py-8">
            <svg className="mx-auto h-12 w-12 text-gray-400" stroke="currentColor" fill="none" viewBox="0 0 48 48">
              <path d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-gray-900">No hay imágenes</h3>
            <p className="mt-1 text-sm text-gray-500">Esta propiedad no tiene imágenes cargadas.</p>
          </div>
        )}

        {property && property.images.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {property.images.map((image) => (
              <div
                key={image.id}
                className="relative group bg-white border border-gray-200 rounded-lg overflow-hidden hover:shadow-md transition-shadow"
              >
                {/* Image */}
                <div className="aspect-video relative bg-gray-100">
                  <Image
                    src={image.imageUrl}
                    alt={image.fileName}
                    fill
                    className="object-cover"
                    sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
                  />
                  
                  {/* Delete button overlay */}
                  <button
                    onClick={() => handleDeleteImage(image.id, image.fileName)}
                    disabled={deletingImageId === image.id}
                    className="absolute top-2 right-2 bg-red-600 hover:bg-red-700 text-white rounded-full p-1.5 opacity-0 group-hover:opacity-100 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Eliminar imagen"
                  >
                    {deletingImageId === image.id ? (
                      <div className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
                    ) : (
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    )}
                  </button>

                  {/* Status indicators */}
                  <div className="absolute bottom-2 left-2 flex gap-1">
                    {!image.enabled && (
                      <span className="bg-gray-600 text-white text-xs px-2 py-1 rounded">
                        Deshabilitada
                      </span>
                    )}
                    {image.order === 0 && (
                      <span className="bg-blue-600 text-white text-xs px-2 py-1 rounded">
                        Principal
                      </span>
                    )}
                  </div>
                </div>

                {/* Image info */}
                <div className="p-3">
                  <p className="text-sm font-medium text-gray-900 truncate" title={image.fileName}>
                    {image.fileName}
                  </p>
                  <div className="mt-1 text-xs text-gray-500">
                    <p>Orden: {image.order}</p>
                    <p>Tamaño: {(image.fileSize / 1024 / 1024).toFixed(1)} MB</p>
                    <p>Subida: {new Date(image.createdAt).toLocaleDateString()}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Footer with actions */}
        <div className="mt-6 flex justify-end gap-3 pt-4 border-t border-gray-200">
          <button
            onClick={handleClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            Cerrar
          </button>
          <button
            onClick={loadPropertyImages}
            disabled={loading}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {loading ? 'Actualizando...' : 'Actualizar'}
          </button>
        </div>
      </div>
    </Modal>
  )
}
