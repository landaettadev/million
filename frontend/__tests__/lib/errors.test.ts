import {
  ApiError,
  NetworkError,
  TimeoutError,
  handleApiError,
  getErrorMessage,
  ErrorCodes,
} from '../../lib/errors'

describe('Error Classes', () => {
  describe('ApiError', () => {
    it('creates ApiError correctly', () => {
      const errorResponse = {
        traceId: 'test-trace-id',
        error: 'Validation failed',
        details: ['Name is required', 'Price must be positive'],
        statusCode: 400,
        timestamp: new Date().toISOString(),
      }

      const apiError = new ApiError(errorResponse)

      expect(apiError.message).toBe('Validation failed')
      expect(apiError.statusCode).toBe(400)
      expect(apiError.traceId).toBe('test-trace-id')
      expect(apiError.details).toEqual(['Name is required', 'Price must be positive'])
      expect(apiError.name).toBe('ApiError')
    })
  })

  describe('NetworkError', () => {
    it('creates NetworkError with default message', () => {
      const networkError = new NetworkError()

      expect(networkError.message).toBe('Network connection failed')
      expect(networkError.name).toBe('NetworkError')
    })

    it('creates NetworkError with custom message', () => {
      const networkError = new NetworkError('Custom network error')

      expect(networkError.message).toBe('Custom network error')
      expect(networkError.name).toBe('NetworkError')
    })
  })

  describe('TimeoutError', () => {
    it('creates TimeoutError with default message', () => {
      const timeoutError = new TimeoutError()

      expect(timeoutError.message).toBe('Request timed out')
      expect(timeoutError.name).toBe('TimeoutError')
    })

    it('creates TimeoutError with custom message', () => {
      const timeoutError = new TimeoutError('Custom timeout error')

      expect(timeoutError.message).toBe('Custom timeout error')
      expect(timeoutError.name).toBe('TimeoutError')
    })
  })
})

describe('handleApiError', () => {
  it('rethrows ApiError as is', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Test error',
      statusCode: 400,
      timestamp: new Date().toISOString(),
    })

    expect(() => handleApiError(apiError)).toThrow(apiError)
  })

  it('converts TypeError with fetch to NetworkError', () => {
    const fetchError = new TypeError('fetch is not defined')

    expect(() => handleApiError(fetchError)).toThrow(NetworkError)
    expect(() => handleApiError(fetchError)).toThrow('Unable to connect to the server')
  })

  it('converts regular Error to NetworkError', () => {
    const error = new Error('Regular error')

    expect(() => handleApiError(error)).toThrow(NetworkError)
    expect(() => handleApiError(error)).toThrow('Regular error')
  })

  it('converts unknown error to NetworkError', () => {
    const unknownError = 'string error'

    expect(() => handleApiError(unknownError)).toThrow(NetworkError)
    expect(() => handleApiError(unknownError)).toThrow('An unexpected error occurred')
  })
})

describe('getErrorMessage', () => {
  it('returns detailed message for 400 ApiError', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Validation failed',
      details: ['Name is required', 'Price must be positive'],
      statusCode: 400,
      timestamp: new Date().toISOString(),
    })

    const message = getErrorMessage(apiError)
    expect(message).toBe('Name is required, Price must be positive')
  })

  it('returns generic message for 400 ApiError without details', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Validation failed',
      statusCode: 400,
      timestamp: new Date().toISOString(),
    })

    const message = getErrorMessage(apiError)
    expect(message).toBe('Invalid request parameters')
  })

  it('returns not found message for 404 ApiError', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Not found',
      statusCode: 404,
      timestamp: new Date().toISOString(),
    })

    const message = getErrorMessage(apiError)
    expect(message).toBe('The requested resource was not found')
  })

  it('returns server error message for 500 ApiError', () => {
    const apiError = new ApiError({
      traceId: 'test-trace-id',
      error: 'Internal server error',
      statusCode: 500,
      timestamp: new Date().toISOString(),
    })

    const message = getErrorMessage(apiError)
    expect(message).toBe('Server error. Please try again later')
  })

  it('returns connection message for NetworkError', () => {
    const networkError = new NetworkError('Connection failed')

    const message = getErrorMessage(networkError)
    expect(message).toBe('Unable to connect to the server. Please check your internet connection')
  })

  it('returns timeout message for TimeoutError', () => {
    const timeoutError = new TimeoutError('Request timed out')

    const message = getErrorMessage(timeoutError)
    expect(message).toBe('Request timed out. Please try again')
  })

  it('returns generic message for unknown error', () => {
    const unknownError = 'string error'

    const message = getErrorMessage(unknownError)
    expect(message).toBe('An unexpected error occurred')
  })
})

describe('ErrorCodes', () => {
  it('has correct error codes', () => {
    expect(ErrorCodes.VALIDATION_ERROR).toBe(400)
    expect(ErrorCodes.NOT_FOUND).toBe(404)
    expect(ErrorCodes.INTERNAL_ERROR).toBe(500)
    expect(ErrorCodes.NETWORK_ERROR).toBe('NETWORK_ERROR')
    expect(ErrorCodes.TIMEOUT_ERROR).toBe('TIMEOUT_ERROR')
  })
})
