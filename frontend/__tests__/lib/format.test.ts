import { formatPriceUSD, formatPrice, formatAddress, formatPropertyName, formatDate } from '../../lib/format'

describe('formatPriceUSD', () => {
  it('should format millions correctly', () => {
    expect(formatPriceUSD(1000000)).toBe('$1.0M')
    expect(formatPriceUSD(2500000)).toBe('$2.5M')
    expect(formatPriceUSD(10000000)).toBe('$10M')
    expect(formatPriceUSD(15000000)).toBe('$15M')
  })

  it('should format thousands correctly', () => {
    expect(formatPriceUSD(1000)).toBe('$1.0K')
    expect(formatPriceUSD(2500)).toBe('$2.5K')
    expect(formatPriceUSD(10000)).toBe('$10K')
    expect(formatPriceUSD(15000)).toBe('$15K')
  })

  it('should format small numbers correctly', () => {
    expect(formatPriceUSD(100)).toBe('$100')
    expect(formatPriceUSD(500)).toBe('$500')
    expect(formatPriceUSD(999)).toBe('$999')
  })

  it('should handle zero', () => {
    expect(formatPriceUSD(0)).toBe('$0')
  })

  it('should handle negative numbers', () => {
    expect(formatPriceUSD(-1000)).toBe('$-1,000')
    expect(formatPriceUSD(-1000000)).toBe('$-1,000,000')
  })
})

describe('formatPrice (alias)', () => {
  it('should be the same as formatPriceUSD', () => {
    expect(formatPrice(1000000)).toBe(formatPriceUSD(1000000))
    expect(formatPrice(2500)).toBe(formatPriceUSD(2500))
    expect(formatPrice(100)).toBe(formatPriceUSD(100))
  })
})

describe('formatAddress', () => {
  it('should format address with multiple parts', () => {
    expect(formatAddress('123 Main St, New York, NY 10001')).toBe('123 Main St, New York')
    expect(formatAddress('456 Park Avenue, Manhattan, New York, NY')).toBe('456 Park Avenue, Manhattan')
  })

  it('should handle address with only two parts', () => {
    expect(formatAddress('123 Main St, New York')).toBe('123 Main St, New York')
  })

  it('should handle address with only one part', () => {
    expect(formatAddress('123 Main St')).toBe('123 Main St')
  })

  it('should handle empty string', () => {
    expect(formatAddress('')).toBe('')
  })

  it('should handle address with extra spaces', () => {
    expect(formatAddress('  123 Main St,  New York,  NY  ')).toBe('123 Main St,  New York')
  })
})

describe('formatPropertyName', () => {
  it('should truncate long names', () => {
    const longName = 'This is a very long property name that should be truncated'
    expect(formatPropertyName(longName)).toBe('This is a very long property n...')
  })

  it('should not truncate short names', () => {
    const shortName = 'Luxury Apartment'
    expect(formatPropertyName(shortName)).toBe('Luxury Apartment')
  })

  it('should handle exactly 30 characters', () => {
    const exactName = 'This is exactly thirty characters long'
    expect(formatPropertyName(exactName)).toBe('This is exactly thirty charact...')
  })

  it('should handle empty string', () => {
    expect(formatPropertyName('')).toBe('')
  })

  it('should handle names with special characters', () => {
    const specialName = 'Luxury & Co. - Premium Residences & Apartments Complex'
    expect(formatPropertyName(specialName)).toBe('Luxury & Co. - Premium Residen...')
  })
})

describe('formatDate', () => {
  it('should format date correctly', () => {
    const date = new Date('2024-01-15T12:00:00.000Z')
    expect(formatDate(date)).toMatch(/January 1[45], 2024/) // tolerate TZ
  })

  it('should handle different months', () => {
    const date = new Date('2024-12-25T12:00:00.000Z')
    expect(formatDate(date)).toMatch(/December 2[45], 2024/)
  })

  it('should handle leap year', () => {
    const date = new Date('2024-02-29T12:00:00.000Z')
    expect(formatDate(date)).toMatch(/February 29, 2024/)
  })

  it('should handle single digit days', () => {
    const date = new Date('2024-03-05T12:00:00.000Z')
    expect(formatDate(date)).toMatch(/March [45], 2024/)
  })

  it('should handle different years', () => {
    const date = new Date('2023-06-10T12:00:00.000Z')
    expect(formatDate(date)).toMatch(/June (9|10), 2023/)
  })
})
