import { AxiosError } from 'axios';
import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import authService from '../authService';
import { apiClient } from './apiClient';

/**
 * Configure authentication interceptors for the API client
 */
export const setupAuthInterceptors = () => {
  // Add request interceptor to include auth token
  const requestInterceptor = apiClient.addRequestInterceptor(
    (config: InternalAxiosRequestConfig) => {
      const token = authService.getToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Add response interceptor to handle token refresh
  const responseInterceptor = apiClient.addResponseInterceptor(
    (response: AxiosResponse) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
      
      // If error is 401 and we haven't retried yet
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;
        
        try {
          // Try to refresh the token
          const newToken = await authService.refreshToken();
          
          // Set the new token in the authorization header
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
          }
          
          // Retry the original request
          return apiClient.getAxiosInstance()(originalRequest);
        } catch (refreshError) {
          // Token refresh failed, log the user out
          authService.logout();
          
          // Redirect to login page
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }
      
      // For other errors, just reject the promise
      return Promise.reject(error);
    }
  );

  // Return function to remove interceptors if needed
  return {
    cleanup: () => {
      apiClient.removeRequestInterceptor(requestInterceptor);
      apiClient.removeResponseInterceptor(responseInterceptor);
    }
  };
};

// Initialize the auth interceptors
export const initializeAuthInterceptors = () => {
  return setupAuthInterceptors();
};

export default {
  setupAuthInterceptors,
  initializeAuthInterceptors
}; 