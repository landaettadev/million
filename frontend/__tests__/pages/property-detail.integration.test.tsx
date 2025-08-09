import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PropertyDetailPage from '../../app/properties/[id]/page'
import { server } from '../setup/server'
import { http, HttpResponse } from 'msw'

// Mock next/navigation
const mockPush = jest.fn()
const mockBack = jest.fn()
const useParamsMock = jest.fn(() => ({ id: '507f1f77bcf86cd799439011' }))

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
    back: mockBack,
  }),
  useParams: () => useParamsMock(),
}))

// Mock Gallery component to simplify testing
jest.mock('../../components/property/Gallery', () => ({
  Gallery: ({ images }: { images: string[] }) => (
    <div data-testid="gallery">
      {images.map((image, index) => (
        <img key={index} src={image} alt={`Gallery image ${index + 1}`} />
      ))}
    </div>
  )
}))

describe('Property Detail Page Integration', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
  afterEach(() => {
    server.resetHandlers()
    mockPush.mockClear()
    mockBack.mockClear()
    useParamsMock.mockReset()
    useParamsMock.mockReturnValue({ id: '507f1f77bcf86cd799439011' })
  })
  afterAll(() => server.close())

  it('loads and displays property details', async () => {
    render(<PropertyDetailPage />)

    // Wait for property details to load
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(screen.getByText('123 Park Avenue, New York')).toBeInTheDocument()
    expect(screen.getByText('$2.5M')).toBeInTheDocument()
    expect(screen.getByText('Beautiful luxury penthouse with stunning city views.')).toBeInTheDocument()
  })

  it('displays property specifications correctly', async () => {
    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(screen.getByText(/3 Beds/)).toBeInTheDocument()
    expect(screen.getByText(/2 Baths/)).toBeInTheDocument()
    // Half baths are not displayed in UI; verify other specs
    expect(screen.getByText(/2,500 SqFt/)).toBeInTheDocument()
  })

  it('displays gallery when multiple images exist', async () => {
    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByTestId('gallery')).toBeInTheDocument()
    })

    const galleryImages = screen.getAllByAltText(/Gallery image/i)
    expect(galleryImages).toHaveLength(3)
  })

  it('handles back button click', async () => {
    const user = userEvent.setup()
    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    const backButton = screen.getByRole('button', { name: /back/i })
    await user.click(backButton)

    expect(mockBack).toHaveBeenCalledTimes(1)
  })

  it('displays CTA buttons', async () => {
    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(screen.getByRole('button', { name: /schedule presentation/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /contact agent/i })).toBeInTheDocument()
  })

  it('handles non-existent property', async () => {
    // Mock the useParams to return a non-existent ID
    useParamsMock.mockReturnValue({ id: 'nonexistent' })

    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Not found')).toBeInTheDocument()
    })

    expect(screen.getByText(/does not exist or is no longer available/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /back to list/i })).toBeInTheDocument()
  })

  it('handles API errors gracefully', async () => {
    // Mock API error
    server.use(
      http.get('http://localhost:5244/api/properties/:id', () => {
        return HttpResponse.json(
          {
            traceId: 'test-trace-id',
            error: 'Internal server error',
            statusCode: 500,
            timestamp: new Date().toISOString(),
          },
          { status: 500 }
        )
      })
    )

    render(<PropertyDetailPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
      expect(screen.getByText(/server error/i)).toBeInTheDocument()
    })

    // Check retry button is present
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument()
  })

  it('retries API call when retry button is clicked', async () => {
    let callCount = 0
    
    // Mock API to fail first time, succeed second time
    server.use(
      http.get('http://localhost:5244/api/properties/:id', ({ params }) => {
        callCount++
        if (callCount === 1) {
          return HttpResponse.json(
            {
              traceId: 'test-trace-id',
              error: 'Internal server error',
              statusCode: 500,
              timestamp: new Date().toISOString(),
            },
            { status: 500 }
          )
        }
        
        return HttpResponse.json({
          id: '507f1f77bcf86cd799439011',
          idOwner: '507f1f77bcf86cd799439012',
          name: 'Luxury Penthouse',
          address: '123 Park Avenue, New York',
          price: 2500000,
          image: 'https://picsum.photos/800/600?random=1',
          operationType: 'sale',
          beds: 3,
          baths: 2,
          halfBaths: 1,
          sqft: 2500,
          images: [
            'https://picsum.photos/800/600?random=1',
            'https://picsum.photos/800/600?random=2',
            'https://picsum.photos/800/600?random=3',
          ],
          description: 'Beautiful luxury penthouse with stunning city views.',
        })
      })
    )

    const user = userEvent.setup()
    render(<PropertyDetailPage />)

    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
    })

    // Click retry button
    const retryButton = screen.getByRole('button', { name: /try again/i })
    await user.click(retryButton)

    // Wait for successful retry
    await waitFor(() => {
      expect(screen.getByText('Luxury Penthouse')).toBeInTheDocument()
    })

    expect(callCount).toBe(2)
  })

  it('handles invalid property ID format', async () => {
    // Mock the useParams to return an invalid ID
    useParamsMock.mockReturnValue({ id: 'invalid' })

    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument()
      expect(screen.getByText(/not a valid ObjectId format/i)).toBeInTheDocument()
    })
  })

  it('shows loading state while fetching data', () => {
    render(<PropertyDetailPage />)

    // Should show loading skeletons initially
    expect(screen.getByLabelText('Loading image')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading title')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading address')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading price')).toBeInTheDocument()
  })

  it('navigates back to list when "Back to list" is clicked', async () => {
    // Mock the useParams to return a non-existent ID
    useParamsMock.mockReturnValue({ id: 'nonexistent' })

    const user = userEvent.setup()
    render(<PropertyDetailPage />)

    await waitFor(() => {
      expect(screen.getByText('Not found')).toBeInTheDocument()
    })

    const backToListButton = screen.getByRole('button', { name: /back to list/i })
    await user.click(backToListButton)

    expect(mockPush).toHaveBeenCalledWith('/properties')
  })
})
