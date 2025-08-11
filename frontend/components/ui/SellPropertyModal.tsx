'use client'

import { useState, useEffect } from 'react'
import { Phone, CheckCircle, X } from 'lucide-react'
import { Button } from './Button'
import { Input } from './Input'

interface SellPropertyModalProps {
  isOpen: boolean
  onClose: () => void
}

export function SellPropertyModal({ isOpen, onClose }: SellPropertyModalProps) {
  const [phone, setPhone] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isSubmitted, setIsSubmitted] = useState(false)

  // Handle escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose()
      }
    }

    if (isOpen) {
      document.addEventListener('keydown', handleEscape)
      document.body.style.overflow = 'hidden'
    }

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = 'unset'
    }
  }, [isOpen, onClose])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!phone.trim()) return

    setIsSubmitting(true)
    
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    setIsSubmitting(false)
    setIsSubmitted(true)
    
    // Reset form after showing success message
    setTimeout(() => {
      setIsSubmitted(false)
      setPhone('')
      onClose()
    }, 3000)
  }

  const handleClose = () => {
    if (!isSubmitting) {
      setPhone('')
      setIsSubmitted(false)
      onClose()
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center p-4 pt-20">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={handleClose}
      />
      
      {/* Modal */}
      <div className="relative bg-black/95 backdrop-blur-sm border border-white/20 rounded-2xl p-8 w-full max-w-lg mx-4 shadow-2xl">
        {/* Close button */}
        <button
          onClick={handleClose}
          disabled={isSubmitting}
          className="absolute top-4 right-4 text-white/60 hover:text-white smooth p-2 rounded-full hover:bg-white/10"
          aria-label="Close modal"
        >
          <X className="w-5 h-5" />
        </button>

        {!isSubmitted ? (
          <>
            <div className="text-center mb-8">
              <div className="mx-auto w-20 h-20 bg-white/10 rounded-full flex items-center justify-center mb-6 border border-white/20">
                <Phone className="w-10 h-10 text-white" />
              </div>
              <h2 className="font-serif text-3xl font-light text-white mb-3 tracking-wide">
                Sell Your Unit
              </h2>
              <p className="text-white/80 text-lg leading-relaxed max-w-sm mx-auto">
                Leave us your number to list your property
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-white/90 mb-3 text-left">
                  Phone Number
                </label>
                <Input
                  id="phone"
                  type="tel"
                  placeholder="+1 (555) 123-4567"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  required
                  className="w-full bg-white/5 border-white/20 text-white placeholder:text-white/40 focus:border-white/40 focus:bg-white/10"
                />
              </div>

              <div className="flex gap-4 pt-6">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={handleClose}
                  disabled={isSubmitting}
                  className="flex-1 border-white/30 text-white hover:bg-white hover:text-black"
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={!phone.trim() || isSubmitting}
                  className="flex-1"
                >
                  {isSubmitting ? 'Sending...' : 'Send'}
                </Button>
              </div>
            </form>
          </>
        ) : (
          <div className="text-center py-8">
            <div className="mx-auto w-20 h-20 bg-white/10 rounded-full flex items-center justify-center mb-6 border border-white/20">
              <CheckCircle className="w-10 h-10 text-white" />
            </div>
            <h3 className="font-serif text-2xl font-light text-white mb-3 tracking-wide">
              Thank You!
            </h3>
            <p className="text-white/80 text-lg leading-relaxed">
              An agent will contact you soon to list your property
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
