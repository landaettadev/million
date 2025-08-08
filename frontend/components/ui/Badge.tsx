import { cn } from '@/lib/utils'

interface BadgeProps {
  children?: React.ReactNode
  kind?: 'sale' | 'rent'
  className?: string
  'aria-label'?: string
}

export function Badge({ children, kind = 'sale', className, 'aria-label': ariaLabel }: BadgeProps) {
  const base = 'inline-flex items-center rounded-full px-3 py-1 text-xs uppercase tracking-wide'
  const style = kind === 'sale'
    ? 'bg-white text-black'
    : 'bg-white/10 text-white border border-white/20'

  return (
    <span className={cn(base, style, className)} aria-label={ariaLabel || kind}>
      {children ?? (kind === 'sale' ? 'Sale' : 'Rent')}
    </span>
  )
}
