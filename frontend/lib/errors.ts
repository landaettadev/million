// Error types from API
export interface ApiErrorResponse {
  traceId: string;
  error: string;
  details?: string[];
  statusCode: number;
  timestamp: string;
}

export class ApiError extends Error {
  public readonly statusCode: number;
  public readonly traceId: string;
  public readonly details?: string[];

  constructor(response: ApiErrorResponse) {
    super(response.error);
    this.statusCode = response.statusCode;
    this.traceId = response.traceId;
    this.details = response.details;
    this.name = 'ApiError';
  }
}

// Network error for connectivity issues
export class NetworkError extends Error {
  constructor(message: string = 'Network connection failed') {
    super(message);
    this.name = 'NetworkError';
  }
}

// Timeout error for slow responses
export class TimeoutError extends Error {
  constructor(message: string = 'Request timed out') {
    super(message);
    this.name = 'TimeoutError';
  }
}

// Error handler utility
export function handleApiError(error: unknown): never {
  if (error instanceof ApiError) {
    throw error;
  }

  if (error instanceof TypeError && error.message.includes('fetch')) {
    throw new NetworkError('Unable to connect to the server');
  }

  if (error instanceof Error) {
    throw new NetworkError(error.message);
  }

  throw new NetworkError('An unexpected error occurred');
}

// User-friendly error messages
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.statusCode) {
      case 400:
        return error.details?.join(', ') || 'Invalid request parameters';
      case 404:
        return 'The requested resource was not found';
      case 500:
        return 'Server error. Please try again later';
      default:
        return error.message || 'An error occurred';
    }
  }

  if (error instanceof NetworkError) {
    return 'Unable to connect to the server. Please check your internet connection';
  }

  if (error instanceof TimeoutError) {
    return 'Request timed out. Please try again';
  }

  return 'An unexpected error occurred';
}

// Error codes for specific handling
export const ErrorCodes = {
  VALIDATION_ERROR: 400,
  NOT_FOUND: 404,
  INTERNAL_ERROR: 500,
  NETWORK_ERROR: 'NETWORK_ERROR',
  TIMEOUT_ERROR: 'TIMEOUT_ERROR',
} as const;
