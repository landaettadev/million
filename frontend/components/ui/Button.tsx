import { ButtonHTMLAttributes, forwardRef } from 'react'
import { cn } from '@/lib/utils'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'ghost'
  size?: 'sm' | 'md'
}

const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', "aria-label": ariaLabel, ...props }, ref) => {
    const base = 'inline-flex items-center justify-center rounded-md font-medium smooth focus:outline-none focus:ring-2 focus:ring-white/30 disabled:opacity-50 disabled:cursor-not-allowed'

    const variants: Record<NonNullable<ButtonProps['variant']>, string> = {
      // Primary: white background, black text
      primary: 'bg-white text-black hover:bg-white/90',
      // Ghost: subtle white border, invert on hover
      ghost: 'border border-white/20 text-white hover:bg-white hover:text-black',
    }

    const sizes: Record<NonNullable<ButtonProps['size']>, string> = {
      sm: 'h-9 px-4 text-sm',
      md: 'h-11 px-6 text-base',
    }

    return (
      <button
        className={cn(base, variants[variant], sizes[size], className)}
        aria-label={ariaLabel}
        ref={ref}
        {...props}
      />
    )
  }
)

Button.displayName = 'Button'

export { Button }
