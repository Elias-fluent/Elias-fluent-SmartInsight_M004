import { AxiosError } from 'axios';
import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import { apiClient } from './apiClient';
import { isAxiosError } from './errorHandler';

/**
 * Interface for retry configuration
 */
export interface RetryConfig {
  retries: number;
  retryDelay: number; // in milliseconds
  retryableStatusCodes: number[];
  retryableErrors: string[];
  shouldRetry?: (error: AxiosError) => boolean;
}

/**
 * Default retry configuration
 */
const defaultRetryConfig: RetryConfig = {
  retries: 3,
  retryDelay: 1000,
  retryableStatusCodes: [408, 429, 500, 502, 503, 504],
  retryableErrors: ['ECONNABORTED', 'ETIMEDOUT', 'ENOTFOUND', 'ENETUNREACH']
};

/**
 * Sets up retry interceptor for failed requests
 * @param config - Custom retry configuration
 */
export const setupRetryInterceptor = (customConfig: Partial<RetryConfig> = {}) => {
  // Merge default config with custom config
  const retryConfig: RetryConfig = {
    ...defaultRetryConfig,
    ...customConfig
  };

  // Add response interceptor
  const interceptorId = apiClient.addResponseInterceptor(
    (response: AxiosResponse) => response,
    async (error: unknown) => {
      // If not an axios error, just reject
      if (!isAxiosError(error)) {
        return Promise.reject(error);
      }

      const axiosError = error as AxiosError;
      const requestConfig = axiosError.config as InternalAxiosRequestConfig & { 
        _retryCount?: number; 
        _retry?: boolean; // For auth interceptor
      };
      
      // Skip if this request is already being retried for authentication
      if (requestConfig._retry) {
        return Promise.reject(error);
      }

      // Initialize retry count if not exists
      if (requestConfig._retryCount === undefined) {
        requestConfig._retryCount = 0;
      }

      // Determine if we should retry
      const shouldRetry = 
        // Custom retry condition if provided
        (typeof customConfig.shouldRetry === 'function' && customConfig.shouldRetry(axiosError)) ||
        // Default retry conditions
        (
          requestConfig._retryCount < retryConfig.retries && (
            // Retry on certain status codes
            (axiosError.response && retryConfig.retryableStatusCodes.includes(axiosError.response.status)) ||
            // Retry on specific network errors
            (axiosError.code && retryConfig.retryableErrors.includes(axiosError.code))
          )
        );

      // If should retry, increment counter and retry after delay
      if (shouldRetry) {
        requestConfig._retryCount += 1;
        
        // Calculate exponential backoff delay
        const delay = retryConfig.retryDelay * (2 ** (requestConfig._retryCount - 1));
        
        // Wait for the delay
        await new Promise(resolve => setTimeout(resolve, delay));
        
        // Retry the request
        return apiClient.getAxiosInstance()(requestConfig);
      }

      // If we shouldn't retry, just reject
      return Promise.reject(error);
    }
  );

  // Return function to remove interceptor if needed
  return {
    cleanup: () => {
      apiClient.removeResponseInterceptor(interceptorId);
    }
  };
};

/**
 * Initialize the retry interceptor with default configuration
 */
export const initializeRetryInterceptor = () => {
  return setupRetryInterceptor();
};

export default {
  setupRetryInterceptor,
  initializeRetryInterceptor
}; 