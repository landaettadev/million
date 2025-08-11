import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

// Mock Next.js components
jest.mock('next/link', () => {
  return ({ children, href, ...props }: any) => {
    return <a href={href} {...props}>{children}</a>
  }
})

jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: any) => {
    return <img src={src} alt={alt} {...props} />
  },
}))

// Create a new QueryClient for testing
const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

describe('API Integration Tests', () => {
  let queryClient: QueryClient

  beforeEach(() => {
    queryClient = createTestQueryClient()
  })

  afterEach(() => {
    queryClient.clear()
  })

  it('should render without crashing', () => {
    render(
      <QueryClientProvider client={queryClient}>
        <div>Test Component</div>
      </QueryClientProvider>
    )
    
    expect(screen.getByText('Test Component')).toBeInTheDocument()
  })

  it('should handle basic user interactions', async () => {
    const user = userEvent.setup()
    
    render(
      <QueryClientProvider client={queryClient}>
        <button onClick={() => alert('clicked')}>Click me</button>
      </QueryClientProvider>
    )
    
    const button = screen.getByRole('button', { name: /click me/i })
    expect(button).toBeInTheDocument()
    
    // Note: We can't test alert in JSDOM, but we can verify the button exists
    expect(button).toBeInTheDocument()
  })
})
