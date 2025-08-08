import { InputHTMLAttributes, forwardRef } from 'react'
import { cn } from '@/lib/utils'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, label, error, id, "aria-label": ariaLabel, ...props }, ref) => {
    const inputId = id || (label ? `${label.replace(/\s+/g, '-').toLowerCase()}-input` : undefined)

    return (
      <div className="space-y-2">
        {label && (
          <label htmlFor={inputId} className="block text-sm text-white/70">
            {label}
          </label>
        )}
        <input
          id={inputId}
          aria-label={ariaLabel || label}
          className={cn(
            'w-full h-11 px-4 rounded-md bg-white/5 text-white placeholder-white/40 border border-white/10 focus:outline-none focus:ring-2 focus:ring-white/30 focus:border-transparent smooth',
            error && 'border-red-500/60 focus:ring-red-500/60',
            className
          )}
          ref={ref}
          {...props}
        />
        {error && <p className="text-sm text-red-400">{error}</p>}
      </div>
    )
  }
)

Input.displayName = 'Input'

export { Input }
