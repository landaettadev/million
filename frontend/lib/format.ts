export function formatPriceUSD(value: number): string {
  if (value >= 1_000_000) {
    const v = value / 1_000_000
    return `$${v.toFixed(v < 10 ? 1 : 0)}M`
  }
  if (value >= 1_000) {
    const v = value / 1_000
    return `$${v.toFixed(v < 10 ? 1 : 0)}K`
  }
  return `$${value.toLocaleString('en-US')}`
}

// Backward-compatibility alias
export const formatPrice = formatPriceUSD

export const formatAddress = (address: string): string => {
  return address.split(',').slice(0, 2).join(',').trim()
}

export const formatPropertyName = (name: string): string => {
  return name.length > 30 ? `${name.substring(0, 30)}...` : name
}

export const formatDate = (date: Date): string => {
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}
