import { HTMLAttributes, forwardRef } from 'react'
import { cn } from '@/lib/utils'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  asChild?: boolean
}

const Card = forwardRef<HTMLDivElement, CardProps>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      className={cn('rounded-2xl bg-[#0c0c0c] border border-white/10 shadow-[0_6px_24px_-8px_rgba(0,0,0,0.5)]', className)}
      {...props}
    />
  )
})

Card.displayName = 'Card'

export { Card }
