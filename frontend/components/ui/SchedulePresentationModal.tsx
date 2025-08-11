'use client'

import { useState } from 'react'
import { Button } from './Button'

interface SchedulePresentationModalProps {
  isOpen: boolean
  onClose: () => void
}

export function SchedulePresentationModal({ isOpen, onClose }: SchedulePresentationModalProps) {
  const [selectedDate, setSelectedDate] = useState<string>('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [isSubmitted, setIsSubmitted] = useState(false)

  if (!isOpen) return null

  // Get current date in YYYY-MM-DD format
  const today = new Date().toISOString().split('T')[0]
  
  // Get next 30 days for calendar options
  const getNextDays = () => {
    const days = []
    for (let i = 1; i <= 30; i++) {
      const date = new Date()
      date.setDate(date.getDate() + i)
      days.push(date.toISOString().split('T')[0])
    }
    return days
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (selectedDate && phoneNumber) {
      setIsSubmitted(true)
      // Auto-close after 3 seconds
      setTimeout(() => {
        setIsSubmitted(false)
        onClose()
      }, 3000)
    }
  }

  const handleClose = () => {
    if (!isSubmitted) {
      onClose()
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-start justify-center p-4 pt-20 z-50">
      <div className="bg-white rounded-2xl p-8 max-w-md w-full max-h-[90vh] overflow-y-auto">
        {!isSubmitted ? (
          <>
            <div className="text-center mb-6">
              <h2 className="font-serif text-2xl font-semibold text-gray-900 mb-2">
                Schedule Presentation
              </h2>
              <p className="text-gray-600">
                Select a date and provide your contact information
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Date Selection */}
              <div>
                <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-2">
                  Select Date
                </label>
                <select
                  id="date"
                  value={selectedDate}
                  onChange={(e) => setSelectedDate(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-black"
                  required
                >
                  <option value="">Choose a date</option>
                  {getNextDays().map((date) => (
                    <option key={date} value={date}>
                      {new Date(date).toLocaleDateString('en-US', {
                        weekday: 'long',
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric'
                      })}
                    </option>
                  ))}
                </select>
              </div>

              {/* Phone Number */}
              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-2">
                  Phone Number
                </label>
                <input
                  type="tel"
                  id="phone"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  placeholder="(555) 123-4567"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  required
                />
              </div>

              {/* Submit Button */}
              <Button
                type="submit"
                variant="primary"
                className="w-full border-2 border-black"
                disabled={!selectedDate || !phoneNumber}
              >
                Confirm Schedule
              </Button>
            </form>

            {/* Close Button */}
            <button
              onClick={handleClose}
              className="absolute top-4 right-4 text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close modal"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </>
        ) : (
          /* Success Message */
          <div className="text-center py-8">
            <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h3 className="font-serif text-xl font-semibold text-gray-900 mb-2">
              Schedule Confirmed!
            </h3>
            <p className="text-gray-600 mb-4">
              Your presentation has been scheduled for {new Date(selectedDate).toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
              })}
            </p>
            <p className="text-gray-600">
              We will contact you soon at {phoneNumber} to confirm the details.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}
