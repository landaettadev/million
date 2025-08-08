import { render, screen, fireEvent } from '@testing-library/react'
import { ErrorMessage, ErrorMessageInline } from '../../../components/ui/ErrorMessage'
import { ApiError, NetworkError, TimeoutError } from '../../../lib/errors'

describe('ErrorMessage Component', () => {
  it('renders API error correctly', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Validation failed',
      details: ['Name is required', 'Price must be positive'],
      statusCode: 400,
      timestamp: new Date().toISOString(),
    })

    render(<ErrorMessage error={apiError} />)
    
    expect(screen.getByText('Error')).toBeInTheDocument()
    expect(screen.getByText('Name is required, Price must be positive')).toBeInTheDocument()
  })

  it('renders network error correctly', () => {
    const networkError = new NetworkError('Connection failed')

    render(<ErrorMessage error={networkError} />)
    
    // The error detection logic checks string content, so for NetworkError it will show "Error" title
    expect(screen.getByText('Error')).toBeInTheDocument()
    expect(screen.getByText('Unable to connect to the server. Please check your internet connection')).toBeInTheDocument()
  })

  it('renders timeout error correctly', () => {
    const timeoutError = new TimeoutError('Request timed out')

    render(<ErrorMessage error={timeoutError} />)
    
    // The error detection logic checks string content, so for TimeoutError it will show "Error" title  
    expect(screen.getByText('Error')).toBeInTheDocument()
    expect(screen.getByText('Request timed out. Please try again')).toBeInTheDocument()
  })

  it('renders generic error correctly', () => {
    const genericError = new Error('Something went wrong')

    render(<ErrorMessage error={genericError} />)
    
    expect(screen.getByText('Error')).toBeInTheDocument()
    expect(screen.getByText('An unexpected error occurred')).toBeInTheDocument()
  })

  it('shows retry button when onRetry is provided', () => {
    const onRetry = jest.fn()
    const error = new Error('Test error')

    render(<ErrorMessage error={error} onRetry={onRetry} />)
    
    const retryButton = screen.getByRole('button', { name: /try again/i })
    expect(retryButton).toBeInTheDocument()
    
    fireEvent.click(retryButton)
    expect(onRetry).toHaveBeenCalledTimes(1)
  })

  it('does not show retry button when onRetry is not provided', () => {
    const error = new Error('Test error')

    render(<ErrorMessage error={error} />)
    
    expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument()
  })

  it('applies custom className', () => {
    const error = new Error('Test error')

    const { container } = render(<ErrorMessage error={error} className="custom-class" />)
    
    // The custom className is applied to the outermost div
    const errorContainer = container.firstChild
    expect(errorContainer).toHaveClass('custom-class')
  })
})

describe('ErrorMessageInline Component', () => {
  it('renders inline error correctly', () => {
    const error = new Error('Inline error')

    render(<ErrorMessageInline error={error} />)
    
    expect(screen.getByText('An unexpected error occurred')).toBeInTheDocument()
  })

  it('shows inline retry button when onRetry is provided', () => {
    const onRetry = jest.fn()
    const error = new Error('Test error')

    render(<ErrorMessageInline error={error} onRetry={onRetry} />)
    
    const retryButton = screen.getByRole('button', { name: /retry/i })
    expect(retryButton).toBeInTheDocument()
    
    fireEvent.click(retryButton)
    expect(onRetry).toHaveBeenCalledTimes(1)
  })
})
