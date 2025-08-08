import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Pagination } from '@/components/property/Pagination'

const mockOnChange = jest.fn()

const renderPagination = (props: {
  page: number
  pageSize: number
  total: number
}) => {
  return render(
    <Pagination
      page={props.page}
      pageSize={props.pageSize}
      total={props.total}
      onChange={mockOnChange}
    />
  )
}

describe('Pagination', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders pagination controls', () => {
    renderPagination({ page: 1, pageSize: 10, total: 100 })
    
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeInTheDocument()
  })

  it('displays current page as active', () => {
    renderPagination({ page: 3, pageSize: 10, total: 100 })
    
    const currentPageButton = screen.getByRole('button', { name: 'Page 3' })
    expect(currentPageButton).toBeInTheDocument()
    expect(currentPageButton).toHaveClass('primary')
  })

  it('shows all pages when total pages is small', () => {
    renderPagination({ page: 1, pageSize: 10, total: 50 })
    
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 2' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 3' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 4' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 5' })).toBeInTheDocument()
  })

  it('shows ellipsis when there are many pages', () => {
    renderPagination({ page: 1, pageSize: 10, total: 200 })
    
    expect(screen.getByText('...')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 2' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 3' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 4' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 5' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 20' })).toBeInTheDocument()
  })

  it('disables previous button on first page', () => {
    renderPagination({ page: 1, pageSize: 10, total: 100 })
    
    const prevButton = screen.getByRole('button', { name: 'Previous page' })
    expect(prevButton).toBeDisabled()
  })

  it('disables next button on last page', () => {
    renderPagination({ page: 10, pageSize: 10, total: 100 })
    
    const nextButton = screen.getByRole('button', { name: 'Next page' })
    expect(nextButton).toBeDisabled()
  })

  it('enables navigation buttons when not on first/last page', () => {
    renderPagination({ page: 5, pageSize: 10, total: 100 })
    
    const prevButton = screen.getByRole('button', { name: 'Previous page' })
    const nextButton = screen.getByRole('button', { name: 'Next page' })
    
    expect(prevButton).not.toBeDisabled()
    expect(nextButton).not.toBeDisabled()
  })

  it('calls onChange when page button is clicked', async () => {
    const user = userEvent.setup()
    renderPagination({ page: 1, pageSize: 10, total: 100 })
    
    const page2Button = screen.getByRole('button', { name: 'Page 2' })
    await user.click(page2Button)
    
    expect(mockOnChange).toHaveBeenCalledWith(2)
  })

  it('calls onChange when previous button is clicked', async () => {
    const user = userEvent.setup()
    renderPagination({ page: 3, pageSize: 10, total: 100 })
    
    const prevButton = screen.getByRole('button', { name: 'Previous page' })
    await user.click(prevButton)
    
    expect(mockOnChange).toHaveBeenCalledWith(2)
  })

  it('calls onChange when next button is clicked', async () => {
    const user = userEvent.setup()
    renderPagination({ page: 3, pageSize: 10, total: 100 })
    
    const nextButton = screen.getByRole('button', { name: 'Next page' })
    await user.click(nextButton)
    
    expect(mockOnChange).toHaveBeenCalledWith(4)
  })

  it('handles edge case with single page', () => {
    renderPagination({ page: 1, pageSize: 10, total: 5 })
    
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled()
  })

  it('handles edge case with zero total', () => {
    renderPagination({ page: 1, pageSize: 10, total: 0 })
    
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled()
  })

  it('shows correct page numbers for middle pages', () => {
    renderPagination({ page: 8, pageSize: 10, total: 200 })
    
    // Should show: 1, ..., 7, 8, 9, ..., 20
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 7' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 8' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 9' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 20' })).toBeInTheDocument()
    expect(screen.getAllByText('...')).toHaveLength(2)
  })

  it('shows correct page numbers for pages near the end', () => {
    renderPagination({ page: 18, pageSize: 10, total: 200 })
    
    // Should show: 1, ..., 16, 17, 18, 19, 20
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 16' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 17' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 18' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 19' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 20' })).toBeInTheDocument()
    expect(screen.getByText('...')).toBeInTheDocument()
  })

  it('has correct accessibility attributes', () => {
    renderPagination({ page: 1, pageSize: 10, total: 100 })
    
    expect(screen.getByRole('button', { name: 'Previous page' })).toHaveAttribute('aria-label', 'Previous page')
    expect(screen.getByRole('button', { name: 'Next page' })).toHaveAttribute('aria-label', 'Next page')
    expect(screen.getByRole('button', { name: 'Page 1' })).toHaveAttribute('aria-label', 'Page 1')
  })

  it('handles large page sizes correctly', () => {
    renderPagination({ page: 1, pageSize: 100, total: 150 })
    
    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 2' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Page 3' })).not.toBeInTheDocument()
  })
})
