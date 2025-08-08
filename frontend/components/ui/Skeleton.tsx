import { cn } from '@/lib/utils'

interface SkeletonProps {
  className?: string
  'aria-label'?: string
}

export function Skeleton({ className, 'aria-label': ariaLabel }: SkeletonProps) {
  return (
    <div
      role="status"
      aria-label={ariaLabel || 'Loading'}
      className={cn('animate-pulse rounded-md bg-white/10', className)}
    />
  )
}
