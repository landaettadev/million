import { SelectHTMLAttributes, forwardRef } from 'react'
import { cn } from '@/lib/utils'

interface Option {
  value: string
  label: string
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options?: Option[]
}

const defaultOptions: Option[] = [
  { value: '', label: 'All' },
  { value: 'sale', label: 'Sale' },
  { value: 'rent', label: 'Rent' },
]

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, label, error, options = defaultOptions, id, "aria-label": ariaLabel, ...props }, ref) => {
    const selectId = id || (label ? `${label.replace(/\s+/g, '-').toLowerCase()}-select` : undefined)

    return (
      <div className="space-y-2">
        {label && (
          <label htmlFor={selectId} className="block text-sm text-white/70">
            {label}
          </label>
        )}
        <select
          id={selectId}
          aria-label={ariaLabel || label}
          className={cn(
            'w-full h-11 px-4 rounded-md bg-white/5 text-white border border-white/10 focus:outline-none focus:ring-2 focus:ring-white/30 focus:border-transparent smooth',
            error && 'border-red-500/60 focus:ring-red-500/60',
            className
          )}
          ref={ref}
          {...props}
        >
          {options.map((o) => (
            <option key={o.value} value={o.value} className="bg-black text-white">
              {o.label}
            </option>
          ))}
        </select>
        {error && <p className="text-sm text-red-400">{error}</p>}
      </div>
    )
  }
)

Select.displayName = 'Select'

export { Select }
