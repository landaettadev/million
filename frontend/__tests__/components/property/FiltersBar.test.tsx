import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FiltersBar, type FiltersValue } from '@/components/property/FiltersBar'

const mockOnChange = jest.fn()
const mockOnSubmit = jest.fn()
const mockOnClear = jest.fn()

const defaultFilters: FiltersValue = {
  name: '',
  address: '',
  minPrice: undefined,
  maxPrice: undefined,
  operationType: ''
}

const renderFiltersBar = (filters: FiltersValue = defaultFilters) => {
  return render(
    <FiltersBar
      value={filters}
      onChange={mockOnChange}
      onSubmit={mockOnSubmit}
      onClear={mockOnClear}
    />
  )
}

describe('FiltersBar', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders all filter inputs', () => {
    renderFiltersBar()
    
    expect(screen.getByLabelText('Filter by name')).toBeInTheDocument()
    expect(screen.getByLabelText('Filter by address')).toBeInTheDocument()
    expect(screen.getByLabelText('Filter by minimum price')).toBeInTheDocument()
    expect(screen.getByLabelText('Filter by maximum price')).toBeInTheDocument()
    expect(screen.getByLabelText('Filter by operation type')).toBeInTheDocument()
  })

  it('renders search and clear buttons', () => {
    renderFiltersBar()
    
    expect(screen.getByRole('button', { name: 'Search' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Clear filters' })).toBeInTheDocument()
  })

  it('displays current filter values', () => {
    const filters: FiltersValue = {
      name: 'Luxury',
      address: 'Park Avenue',
      minPrice: 1000000,
      maxPrice: 5000000,
      operationType: 'sale'
    }
    
    renderFiltersBar(filters)
    
    expect(screen.getByDisplayValue('Luxury')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Park Avenue')).toBeInTheDocument()
    expect(screen.getByDisplayValue('1000000')).toBeInTheDocument()
    expect(screen.getByDisplayValue('5000000')).toBeInTheDocument()
    const operationSelect = screen.getByLabelText('Filter by operation type') as HTMLSelectElement
    expect(operationSelect.value).toBe('sale')
  })

  it('calls onChange when name input changes', async () => {
    renderFiltersBar()
    const nameInput = screen.getByLabelText('Filter by name')
    fireEvent.change(nameInput, { target: { value: 'Luxury' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, name: 'Luxury' })
  })

  it('calls onChange when address input changes', async () => {
    renderFiltersBar()
    const addressInput = screen.getByLabelText('Filter by address')
    fireEvent.change(addressInput, { target: { value: 'Park Avenue' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, address: 'Park Avenue' })
  })

  it('calls onChange when min price input changes', async () => {
    renderFiltersBar()
    const minPriceInput = screen.getByLabelText('Filter by minimum price')
    fireEvent.change(minPriceInput, { target: { value: '1000000' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, minPrice: 1000000 })
  })

  it('calls onChange when max price input changes', async () => {
    renderFiltersBar()
    const maxPriceInput = screen.getByLabelText('Filter by maximum price')
    fireEvent.change(maxPriceInput, { target: { value: '5000000' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, maxPrice: 5000000 })
  })

  it('calls onChange when operation type changes', async () => {
    const user = userEvent.setup()
    renderFiltersBar()
    
    const operationSelect = screen.getByLabelText('Filter by operation type')
    await user.selectOptions(operationSelect, 'sale')
    
    expect(mockOnChange).toHaveBeenCalledWith({
      ...defaultFilters,
      operationType: 'sale'
    })
  })

  it('handles invalid number inputs gracefully', async () => {
    renderFiltersBar({ ...defaultFilters, minPrice: 1 })
    const minPriceInput = screen.getByLabelText('Filter by minimum price')
    // number inputs ignore non-numeric strings; simulate clearing to become undefined
    fireEvent.change(minPriceInput, { target: { value: '' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, minPrice: undefined })
  })

  it('calls onSubmit when form is submitted', async () => {
    const user = userEvent.setup()
    renderFiltersBar()
    
    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)
    
    expect(mockOnSubmit).toHaveBeenCalledTimes(1)
  })

  it('calls onClear when clear button is clicked', async () => {
    const user = userEvent.setup()
    renderFiltersBar()
    
    const clearButton = screen.getByRole('button', { name: 'Clear filters' })
    await user.click(clearButton)
    
    expect(mockOnClear).toHaveBeenCalledTimes(1)
  })

  it('prevents default form submission', async () => {
    const user = userEvent.setup()
    renderFiltersBar()
    
    const submitBtn = screen.getByRole('button', { name: 'Search' })
    await user.click(submitBtn)
    expect(mockOnSubmit).toHaveBeenCalledTimes(1)
  })

  it('handles empty string inputs correctly', async () => {
    renderFiltersBar({ ...defaultFilters, name: 'A' })
    const nameInput = screen.getByLabelText('Filter by name')
    fireEvent.change(nameInput, { target: { value: '' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, name: undefined })
  })

  it('handles number inputs with empty strings', async () => {
    renderFiltersBar({ ...defaultFilters, minPrice: 1 })
    const minPriceInput = screen.getByLabelText('Filter by minimum price')
    fireEvent.change(minPriceInput, { target: { value: '' } })
    expect(mockOnChange).toHaveBeenLastCalledWith({ ...defaultFilters, minPrice: undefined })
  })

  it('has correct accessibility attributes', () => {
    renderFiltersBar()
    
    expect(screen.getByLabelText('Filter by name')).toHaveAttribute('aria-label', 'Filter by name')
    expect(screen.getByLabelText('Filter by address')).toHaveAttribute('aria-label', 'Filter by address')
    expect(screen.getByLabelText('Filter by minimum price')).toHaveAttribute('aria-label', 'Filter by minimum price')
    expect(screen.getByLabelText('Filter by maximum price')).toHaveAttribute('aria-label', 'Filter by maximum price')
    expect(screen.getByLabelText('Filter by operation type')).toHaveAttribute('aria-label', 'Filter by operation type')
  })
})
