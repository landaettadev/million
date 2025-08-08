import { Button } from '@/components/ui/Button'

interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (p: number) => void
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const canPrev = page > 1
  const canNext = page < totalPages

  const buildPages = () => {
    const pages: (number | '...')[] = []
    const max = 7
    if (totalPages <= max) {
      for (let i = 1; i <= totalPages; i++) pages.push(i)
      return pages
    }

    if (page <= 4) {
      pages.push(1, 2, 3, 4, 5, '...', totalPages)
    } else if (page >= totalPages - 3) {
      pages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages)
    } else {
      pages.push(1, '...', page - 1, page, page + 1, '...', totalPages)
    }
    return pages
  }

  return (
    <div className="flex items-center justify-center gap-3 py-10">
      <Button variant="ghost" size="md" onClick={() => onChange(page - 1)} disabled={!canPrev} aria-label="Previous page">
        Prev
      </Button>

      {buildPages().map((p, idx) =>
        p === '...'
          ? (
              <span key={`dots-${idx}`} className="px-2 text-white/50">
                ...
              </span>
            )
          : (
              <Button
                key={p}
                variant={p === page ? 'primary' : 'ghost'}
                size="md"
                onClick={() => onChange(p)}
                aria-label={`Page ${p}`}
              >
                {p}
              </Button>
            )
      )}

      <Button variant="ghost" size="md" onClick={() => onChange(page + 1)} disabled={!canNext} aria-label="Next page">
        Next
      </Button>
    </div>
  )
}
