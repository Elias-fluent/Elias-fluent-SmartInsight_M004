// Export the main API client
export { apiClient, ApiClient } from './apiClient';
export type { ApiClientConfig } from './apiClient';

// Export authentication handling
import authInterceptor from './authInterceptor';
export const { setupAuthInterceptors, initializeAuthInterceptors } = authInterceptor;

// Export error handling utilities
export { 
  formatApiError, 
  isAxiosError, 
  getUserFriendlyErrorMessage,
  ApiErrorType,
} from './errorHandler';
export type { ApiError } from './errorHandler';

// Export retry functionality
import retryInterceptor from './retryInterceptor';
export const { setupRetryInterceptor, initializeRetryInterceptor } = retryInterceptor;
export type { RetryConfig } from './retryInterceptor';

// Create an initialize function to set up all interceptors
export const initializeApiClient = () => {
  // Initialize auth interceptors
  const authInterceptors = initializeAuthInterceptors();
  
  // Initialize retry interceptors
  const retryInterceptors = initializeRetryInterceptor();
  
  // Return cleanup function
  return () => {
    authInterceptors.cleanup();
    retryInterceptors.cleanup();
  };
};

// Default export
export default {
  initializeApiClient
}; 