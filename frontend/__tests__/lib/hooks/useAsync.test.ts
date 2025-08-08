import { renderHook, act } from '@testing-library/react'
import { useAsync, useApiCall } from '../../../lib/hooks/useAsync'

describe('useAsync', () => {
  it('initializes with correct default state', () => {
    const asyncFunction = jest.fn()
    const { result } = renderHook(() => useAsync(asyncFunction))

    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('executes function successfully', async () => {
    const mockData = { id: 1, name: 'Test' }
    const asyncFunction = jest.fn().mockResolvedValue(mockData)
    const { result } = renderHook(() => useAsync(asyncFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.data).toEqual(mockData)
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
    expect(asyncFunction).toHaveBeenCalledTimes(1)
  })

  it('handles function errors correctly', async () => {
    const errorMessage = 'Test error'
    const asyncFunction = jest.fn().mockRejectedValue(new Error(errorMessage))
    const { result } = renderHook(() => useAsync(asyncFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toEqual(new Error(errorMessage))
  })

  it('sets loading state correctly during execution', async () => {
    let resolve: (value: any) => void
    const promise = new Promise((res) => { resolve = res })
    const asyncFunction = jest.fn().mockReturnValue(promise)
    const { result } = renderHook(() => useAsync(asyncFunction))

    act(() => {
      result.current.execute()
    })

    expect(result.current.loading).toBe(true)
    expect(result.current.error).toBeNull()

    await act(async () => {
      resolve('test data')
      await promise
    })

    expect(result.current.loading).toBe(false)
  })

  it('executes immediately when immediate is true', async () => {
    const mockData = { id: 1, name: 'Test' }
    const asyncFunction = jest.fn().mockResolvedValue(mockData)
    
    const { result } = renderHook(() => useAsync(asyncFunction, true))

    // Wait for the immediate execution to complete
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0))
    })

    expect(asyncFunction).toHaveBeenCalledTimes(1)
    expect(result.current.data).toEqual(mockData)
  })

  it('resets state correctly', async () => {
    const mockData = { id: 1, name: 'Test' }
    const asyncFunction = jest.fn().mockResolvedValue(mockData)
    const { result } = renderHook(() => useAsync(asyncFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.data).toEqual(mockData)

    act(() => {
      result.current.reset()
    })

    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('passes arguments to async function', async () => {
    const asyncFunction = jest.fn().mockResolvedValue('result')
    const { result } = renderHook(() => useAsync(asyncFunction))

    await act(async () => {
      await result.current.execute('arg1', 'arg2', 123)
    })

    expect(asyncFunction).toHaveBeenCalledWith('arg1', 'arg2', 123)
  })
})

describe('useApiCall', () => {
  it('initializes with correct default state', () => {
    const apiFunction = jest.fn()
    const { result } = renderHook(() => useApiCall(apiFunction))

    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
    expect(result.current.retryCount).toBe(0)
    expect(result.current.canRetry).toBe(false)
    expect(result.current.maxRetries).toBe(3)
  })

  it('resets retry count on new execution', async () => {
    const apiFunction = jest.fn()
      .mockRejectedValueOnce(new Error('First error'))  // First execute() call fails
      .mockRejectedValueOnce(new Error('Retry error'))  // First retry() call fails  
      .mockResolvedValue('success')                      // Second execute() call succeeds
    
    const { result } = renderHook(() => useApiCall(apiFunction))

    // First execution fails
    await act(async () => {
      await result.current.execute().catch(() => {})
    })

    expect(result.current.retryCount).toBe(0)
    expect(result.current.canRetry).toBe(true)

    // Retry also fails
    await act(async () => {
      await result.current.retry().catch(() => {})
    })

    expect(result.current.retryCount).toBe(1)
    expect(result.current.error).not.toBeNull()

    // New execution resets retry count and succeeds
    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.retryCount).toBe(0)
    expect(result.current.data).toBe('success')
    expect(result.current.error).toBeNull()
  })

  it('handles retry correctly', async () => {
    const apiFunction = jest.fn()
      .mockRejectedValueOnce(new Error('First error'))
      .mockResolvedValue('success')
    
    const { result } = renderHook(() => useApiCall(apiFunction))

    // First execution fails
    await act(async () => {
      await result.current.execute().catch(() => {})
    })

    expect(result.current.error).not.toBeNull()
    expect(result.current.canRetry).toBe(true)
    expect(result.current.retryCount).toBe(0)

    // Retry succeeds
    await act(async () => {
      await result.current.retry()
    })

    expect(result.current.data).toBe('success')
    expect(result.current.error).toBeNull()
    expect(result.current.retryCount).toBe(1)
    expect(result.current.canRetry).toBe(false)
  })

  it('respects max retry limit', async () => {
    const apiFunction = jest.fn().mockRejectedValue(new Error('Always fails'))
    const { result } = renderHook(() => useApiCall(apiFunction, false, 2))

    // First execution
    await act(async () => {
      await result.current.execute().catch(() => {})
    })

    expect(result.current.canRetry).toBe(true)

    // First retry
    await act(async () => {
      await result.current.retry().catch(() => {})
    })

    expect(result.current.retryCount).toBe(1)
    expect(result.current.canRetry).toBe(true)

    // Second retry
    await act(async () => {
      await result.current.retry().catch(() => {})
    })

    expect(result.current.retryCount).toBe(2)
    expect(result.current.canRetry).toBe(false)

    // Third retry should not be possible
    await act(async () => {
      await result.current.retry().catch(() => {})
    })

    expect(result.current.retryCount).toBe(2) // Should not increase
  })

  it('canRetry is false when there is no error', async () => {
    const apiFunction = jest.fn().mockResolvedValue('success')
    const { result } = renderHook(() => useApiCall(apiFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.error).toBeNull()
    expect(result.current.canRetry).toBe(false)
  })
})
