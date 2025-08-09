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
  default: ({ fill, ...rest }) => {
    // eslint-disable-next-line @next/next/no-img-element
    return <img {...rest} />
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

// Minimal polyfill to silence TransformStream warnings under Node
if (typeof global.TransformStream === 'undefined') {
  try {
    const { TransformStream } = require('stream/web')
    global.TransformStream = TransformStream
  } catch {
    // fallback minimal stub if stream/web is not available
    global.TransformStream = class TransformStream {}
  }
}

// Polyfill BroadcastChannel for MSW in Node test env
if (typeof global.BroadcastChannel === 'undefined') {
  // minimal no-op polyfill sufficient for MSW internals
  global.BroadcastChannel = class {
    constructor() {}
    postMessage() {}
    close() {}
    addEventListener() {}
    removeEventListener() {}
  }
}

// Note: MSW is configured per-suite in integration tests to avoid interference with unit tests
