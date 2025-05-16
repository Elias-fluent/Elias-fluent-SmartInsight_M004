import { AxiosError } from 'axios';

/**
 * Error types for API responses
 */
export enum ApiErrorType {
  NETWORK = 'network',
  TIMEOUT = 'timeout',
  SERVER = 'server',
  UNAUTHORIZED = 'unauthorized',
  FORBIDDEN = 'forbidden',
  NOT_FOUND = 'not_found',
  VALIDATION = 'validation',
  CONFLICT = 'conflict',
  UNKNOWN = 'unknown'
}

/**
 * Interface for formatted API errors
 */
export interface ApiError {
  type: ApiErrorType;
  status?: number;
  message: string;
  originalError: Error | AxiosError;
  validationErrors?: Record<string, string[]>;
}

/**
 * Formats API errors into a consistent structure
 * @param error - The error to format
 * @returns Formatted API error
 */
export function formatApiError(error: unknown): ApiError {
  // Default error
  const defaultError: ApiError = {
    type: ApiErrorType.UNKNOWN,
    message: 'An unexpected error occurred',
    originalError: error instanceof Error ? error : new Error(String(error))
  };

  // If not an Axios error, return default error
  if (!isAxiosError(error)) {
    // Handle network errors
    if (error instanceof Error && error.message.includes('Network Error')) {
      return {
        ...defaultError,
        type: ApiErrorType.NETWORK,
        message: 'Unable to connect to the server. Please check your internet connection.'
      };
    }
    return defaultError;
  }

  // Handle Axios errors
  const axiosError = error as AxiosError;
  const status = axiosError.response?.status;

  // Handle timeout errors
  if (axiosError.code === 'ECONNABORTED') {
    return {
      type: ApiErrorType.TIMEOUT,
      message: 'The request timed out. Please try again.',
      originalError: axiosError
    };
  }

  // Handle errors with response
  if (status) {
    // Get error message from response if available
    const responseData = axiosError.response?.data as any;
    const errorMessage = responseData?.message || responseData?.error || axiosError.message;

    switch (status) {
      case 400:
        return {
          type: ApiErrorType.VALIDATION,
          status,
          message: errorMessage || 'Invalid request data',
          originalError: axiosError,
          validationErrors: responseData?.errors
        };
      case 401:
        return {
          type: ApiErrorType.UNAUTHORIZED,
          status,
          message: errorMessage || 'You are not authorized to perform this action',
          originalError: axiosError
        };
      case 403:
        return {
          type: ApiErrorType.FORBIDDEN,
          status,
          message: errorMessage || 'You do not have permission to access this resource',
          originalError: axiosError
        };
      case 404:
        return {
          type: ApiErrorType.NOT_FOUND,
          status,
          message: errorMessage || 'The requested resource was not found',
          originalError: axiosError
        };
      case 409:
        return {
          type: ApiErrorType.CONFLICT,
          status,
          message: errorMessage || 'The request conflicts with the current state of the resource',
          originalError: axiosError
        };
      case 500:
      case 502:
      case 503:
      case 504:
        return {
          type: ApiErrorType.SERVER,
          status,
          message: errorMessage || 'A server error occurred. Please try again later.',
          originalError: axiosError
        };
      default:
        return {
          type: ApiErrorType.UNKNOWN,
          status,
          message: errorMessage || 'An unexpected error occurred',
          originalError: axiosError
        };
    }
  }

  // Handle errors without response (like network errors)
  return {
    type: ApiErrorType.NETWORK,
    message: 'Unable to connect to the server. Please check your internet connection.',
    originalError: axiosError
  };
}

/**
 * Type guard for AxiosError
 */
export function isAxiosError(error: unknown): error is AxiosError {
  return (
    error !== null &&
    typeof error === 'object' &&
    'isAxiosError' in error &&
    (error as AxiosError).isAxiosError === true
  );
}

/**
 * Returns a user-friendly message for an API error
 */
export function getUserFriendlyErrorMessage(error: unknown): string {
  const formattedError = formatApiError(error);
  return formattedError.message;
}

export default {
  formatApiError,
  isAxiosError,
  getUserFriendlyErrorMessage,
  ApiErrorType
}; 