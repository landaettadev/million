import { useState, useCallback, useEffect } from 'react';

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: unknown | null;
}

interface UseAsyncReturn<T> extends AsyncState<T> {
  execute: (...args: any[]) => Promise<void>;
  reset: () => void;
}

export function useAsync<T>(
  asyncFunction: (...args: any[]) => Promise<T>,
  immediate: boolean = false
): UseAsyncReturn<T> {
  const [state, setState] = useState<AsyncState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const execute = useCallback(
    async (...args: any[]) => {
      setState(prev => ({ ...prev, loading: true, error: null }));
      
      try {
        const result = await asyncFunction(...args);
        setState({ data: result, loading: false, error: null });
      } catch (error) {
        setState({ data: null, loading: false, error });
      }
    },
    [asyncFunction]
  );

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  useEffect(() => {
    if (immediate) {
      execute();
    }
  }, [execute, immediate]);

  return {
    ...state,
    execute,
    reset,
  };
}

// Specialized hook for API calls with retry functionality
export function useApiCall<T>(
  apiFunction: (...args: any[]) => Promise<T>,
  immediate: boolean = false,
  maxRetries: number = 3
) {
  const [retryCount, setRetryCount] = useState(0);
  const asyncResult = useAsync(apiFunction, immediate);

  const executeWithRetry = useCallback(
    async (...args: any[]) => {
      setRetryCount(0);
      await asyncResult.execute(...args);
    },
    [asyncResult]
  );

  const retry = useCallback(
    async (...args: any[]) => {
      if (retryCount < maxRetries) {
        setRetryCount(prev => prev + 1);
        await asyncResult.execute(...args);
      }
    },
    [asyncResult, retryCount, maxRetries]
  );

  const canRetry = retryCount < maxRetries && !!asyncResult.error;

  return {
    ...asyncResult,
    execute: executeWithRetry,
    retry,
    retryCount,
    canRetry,
    maxRetries,
  };
}
