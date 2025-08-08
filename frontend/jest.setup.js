import '@testing-library/jest-dom'
import 'whatwg-fetch'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    }
  },
  useParams() {
    return {}
  },
  useSearchParams() {
    return new URLSearchParams()
  },
  usePathname() {
    return ''
  },
}))

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: (props) => {
    // eslint-disable-next-line @next/next/no-img-element
    return <img {...props} />
  },
}))

// Mock IntersectionObserver
global.IntersectionObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}))

// Mock ResizeObserver
global.ResizeObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}))

// Mock scrollTo
global.scrollTo = jest.fn()

// Polyfill for fetch and Response
import { TextEncoder, TextDecoder } from 'util'
import { ReadableStream } from 'stream/web'

global.TextEncoder = TextEncoder
global.TextDecoder = TextDecoder
global.ReadableStream = ReadableStream
global.Response = Response
global.Request = Request

// Mock fetch for tests
global.fetch = jest.fn()

// Set test environment variables
process.env.NEXT_PUBLIC_API_BASE = 'http://localhost:5244'
process.env.NEXT_PUBLIC_ENVIRONMENT = 'test'

// MSW setup - only import and setup when needed
let server
if (process.env.NODE_ENV === 'test') {
  try {
    const { server: testServer } = require('./__tests__/setup/server')
    server = testServer
    
    beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
    afterEach(() => server.resetHandlers())
    afterAll(() => server.close())
  } catch (error) {
    // MSW setup is optional, only needed for integration tests
    console.warn('MSW setup failed, continuing without mocking:', error.message)
  }
}
