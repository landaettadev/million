'use client'

import { useCallback } from 'react'
import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { Button } from '@/components/ui/Button'

export type FiltersValue = {
  name?: string
  address?: string
  minPrice?: number
  maxPrice?: number
  operationType?: '' | 'sale' | 'rent'
}

interface FiltersBarProps {
  value: FiltersValue
  onChange: (next: FiltersValue) => void
  onSubmit: () => void
  onClear: () => void
}

export function FiltersBar({ value, onChange, onSubmit, onClear }: FiltersBarProps) {
  const setField = useCallback(
    (key: keyof FiltersValue, val: string | number | undefined) => {
      onChange({ ...value, [key]: val })
    },
    [value, onChange]
  )

  const onSubmitForm = (e: React.FormEvent) => {
    e.preventDefault()
    onSubmit()
  }

  const toNumberOrUndefined = (v: string) => {
    if (v === '') return undefined
    const n = Number(v)
    return Number.isNaN(n) ? undefined : n
  }

  return (
    <div className="border-b border-white/10 md:sticky md:top-16 md:z-40 bg-black md:bg-black/80 md:backdrop-blur">
      <div className="container">
        <form onSubmit={onSubmitForm} className="py-4">
          <div className="grid grid-cols-1 md:grid-cols-6 gap-3">
            <Input
              label="Name"
              placeholder="Search by name"
              value={value.name ?? ''}
              onChange={(e) => setField('name', e.target.value || undefined)}
              aria-label="Filter by name"
            />

            <Input
              label="Address"
              placeholder="Search by address"
              value={value.address ?? ''}
              onChange={(e) => setField('address', e.target.value || undefined)}
              aria-label="Filter by address"
            />

            <Input
              label="Min price"
              type="number"
              inputMode="numeric"
              placeholder="0"
              value={value.minPrice?.toString() ?? ''}
              onChange={(e) => setField('minPrice', toNumberOrUndefined(e.target.value))}
              aria-label="Filter by minimum price"
            />

            <Input
              label="Max price"
              type="number"
              inputMode="numeric"
              placeholder="Any"
              value={value.maxPrice?.toString() ?? ''}
              onChange={(e) => setField('maxPrice', toNumberOrUndefined(e.target.value))}
              aria-label="Filter by maximum price"
            />

            <Select
              label="Operation"
              value={value.operationType ?? ''}
              onChange={(e) => setField('operationType', (e.target.value as '' | 'sale' | 'rent') || '')}
              options={[
                { value: '', label: 'All' },
                { value: 'sale', label: 'Sale' },
                { value: 'rent', label: 'Rent' },
              ]}
              aria-label="Filter by operation type"
            />

            <div className="flex items-end gap-3">
              <Button type="submit" variant="primary" size="md" aria-label="Search">
                Search
              </Button>
              <Button type="button" variant="ghost" size="md" onClick={onClear} aria-label="Clear filters">
                Clear
              </Button>
            </div>
          </div>
        </form>
      </div>
    </div>
  )
}
