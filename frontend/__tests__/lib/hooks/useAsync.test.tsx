import { renderHook, act, waitFor } from '@testing-library/react'
import { useAsync, useApiCall } from '../../../lib/hooks/useAsync'

// Mock async function that resolves
const mockAsyncFunction = jest.fn().mockResolvedValue('success')

// Mock async function that rejects
const mockAsyncErrorFunction = jest.fn().mockRejectedValue(new Error('Test error'))

describe('useAsync', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('should initialize with default state', () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
    expect(typeof result.current.execute).toBe('function')
    expect(typeof result.current.reset).toBe('function')
  })

  it('should execute async function and update state', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.data).toBe('success')
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
    expect(mockAsyncFunction).toHaveBeenCalledTimes(1)
  })

  it('should handle errors correctly', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncErrorFunction))
    
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeInstanceOf(Error)
    expect((result.current.error as Error).message).toBe('Test error')
  })

  it('should set loading state during execution', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    act(() => {
      result.current.execute()
    })
    
    expect(result.current.loading).toBe(true)
    
    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })
  })

  it('should reset state when reset is called', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    // First execute to set some data
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.data).toBe('success')
    
    // Reset
    act(() => {
      result.current.reset()
    })
    
    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('should execute immediately when immediate is true', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction, true))
    
    expect(result.current.loading).toBe(true)
    
    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })
    
    expect(result.current.data).toBe('success')
    expect(mockAsyncFunction).toHaveBeenCalledTimes(1)
  })

  it('should pass arguments to async function', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    await act(async () => {
      await result.current.execute('arg1', 'arg2')
    })
    
    expect(mockAsyncFunction).toHaveBeenCalledWith('arg1', 'arg2')
  })

  it('should handle multiple executions', async () => {
    const { result } = renderHook(() => useAsync(mockAsyncFunction))
    
    await act(async () => {
      await result.current.execute()
    })
    
    await act(async () => {
      await result.current.execute()
    })
    
    expect(mockAsyncFunction).toHaveBeenCalledTimes(2)
  })
})

describe('useApiCall', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('should initialize with retry state', () => {
    const { result } = renderHook(() => useApiCall(mockAsyncFunction))
    
    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
    expect(result.current.retryCount).toBe(0)
    expect(result.current.canRetry).toBe(false)
    expect(result.current.maxRetries).toBe(3)
    expect(typeof result.current.retry).toBe('function')
  })

  it('should handle retries correctly', async () => {
    const mockFunction = jest.fn()
      .mockRejectedValueOnce(new Error('First error'))
      .mockRejectedValueOnce(new Error('Second error'))
      .mockResolvedValueOnce('success')
    
    const { result } = renderHook(() => useApiCall(mockFunction))
    
    // First execution fails
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.error).toBeInstanceOf(Error)
    expect(result.current.retryCount).toBe(0)
    expect(result.current.canRetry).toBe(true)
    
    // First retry fails
    await act(async () => {
      await result.current.retry()
    })
    
    expect(result.current.retryCount).toBe(1)
    expect(result.current.canRetry).toBe(true)
    
    // Second retry succeeds
    await act(async () => {
      await result.current.retry()
    })
    
    expect(result.current.data).toBe('success')
    expect(result.current.retryCount).toBe(2)
    expect(result.current.canRetry).toBe(false)
  })

  it('should not retry beyond max retries', async () => {
    const mockFunction = jest.fn().mockRejectedValue(new Error('Always fails'))
    const { result } = renderHook(() => useApiCall(mockFunction, false, 2))
    
    // Initial execution
    await act(async () => {
      await result.current.execute()
    })
    
    // First retry
    await act(async () => {
      await result.current.retry()
    })
    
    // Second retry
    await act(async () => {
      await result.current.retry()
    })
    
    // Third retry should not work
    await act(async () => {
      await result.current.retry()
    })
    
    expect(result.current.retryCount).toBe(2)
    expect(result.current.canRetry).toBe(false)
    expect(mockFunction).toHaveBeenCalledTimes(3) // Initial + 2 retries
  })

  it('should reset retry count on new execution', async () => {
    const mockFunction = jest.fn()
      .mockRejectedValueOnce(new Error('First error'))
      .mockResolvedValueOnce('success')
    
    const { result } = renderHook(() => useApiCall(mockFunction))
    
    // First execution fails
    await act(async () => {
      await result.current.execute()
    })
    
    // Retry once
    await act(async () => {
      await result.current.retry()
    })
    
    expect(result.current.retryCount).toBe(1)
    
    // New execution should reset retry count
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.retryCount).toBe(0)
  })

  it('should handle successful execution without retries', async () => {
    const { result } = renderHook(() => useApiCall(mockAsyncFunction))
    
    await act(async () => {
      await result.current.execute()
    })
    
    expect(result.current.data).toBe('success')
    expect(result.current.retryCount).toBe(0)
    expect(result.current.canRetry).toBe(false)
  })

  it('should respect custom max retries', () => {
    const { result } = renderHook(() => useApiCall(mockAsyncFunction, false, 5))
    
    expect(result.current.maxRetries).toBe(5)
  })
})
