import axios, { isAxiosError } from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { authActions } from '../slices/authSlice';
import { uiActions } from '../slices/uiSlice';

// Define API action types
export const API_REQUEST = 'api/request';
export const API_SUCCESS = 'api/success';
export const API_FAILURE = 'api/failure';

// Define API action interfaces
export interface ApiRequestAction {
  type: typeof API_REQUEST;
  payload: {
    url: string;
    method: 'get' | 'post' | 'put' | 'delete' | 'patch';
    data?: any;
    onSuccess?: string;
    onFailure?: string;
    label?: string;
    config?: AxiosRequestConfig;
  };
}

export interface ApiSuccessAction {
  type: typeof API_SUCCESS;
  payload: {
    response: AxiosResponse;
    originalRequest: ApiRequestAction['payload'];
  };
}

export interface ApiFailureAction {
  type: typeof API_FAILURE;
  payload: {
    error: any;
    originalRequest: ApiRequestAction['payload'];
  };
}

// Export API actions union type
export type ApiAction = ApiRequestAction | ApiSuccessAction | ApiFailureAction;

// Define API action creators
export const apiRequest = (
  url: string,
  method: 'get' | 'post' | 'put' | 'delete' | 'patch',
  data?: any,
  onSuccess?: string,
  onFailure?: string,
  label?: string,
  config?: AxiosRequestConfig
): ApiRequestAction => ({
  type: API_REQUEST,
  payload: {
    url,
    method,
    data,
    onSuccess,
    onFailure,
    label,
    config,
  },
});

export const apiSuccess = (
  response: AxiosResponse,
  originalRequest: ApiRequestAction['payload']
): ApiSuccessAction => ({
  type: API_SUCCESS,
  payload: {
    response,
    originalRequest,
  },
});

export const apiFailure = (
  error: any,
  originalRequest: ApiRequestAction['payload']
): ApiFailureAction => ({
  type: API_FAILURE,
  payload: {
    error,
    originalRequest,
  },
});

// Create axios instance
const createApiClient = (): AxiosInstance => {
  const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || '/api',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Add request interceptor
  api.interceptors.request.use(
    (config) => {
      // Get the token from localStorage
      const token = localStorage.getItem('smartinsight_token');
      
      // If token exists, add it to the authorization header
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Add response interceptor
  api.interceptors.response.use(
    (response) => response,
    (error) => {
      // Handle 401 Unauthorized errors
      if (isAxiosError(error) && error.response?.status === 401) {
        // Clear the token
        localStorage.removeItem('smartinsight_token');
      }
      
      return Promise.reject(error);
    }
  );

  return api;
};

// Create the API middleware
export const apiMiddleware = (store: any) => {
  const axiosInstance = createApiClient();
  
  return (next: any) => async (action: any) => {
    // Pass all non-api actions to the next middleware
    if (!action || typeof action !== 'object' || !('type' in action) || action.type !== API_REQUEST) {
      return next(action);
    }

    // Now that we've confirmed it's an ApiRequestAction, cast it
    const apiAction = action as ApiRequestAction;
    const { url, method, data, onSuccess, onFailure, label, config } = apiAction.payload;
    
    // Dispatch the original API request action
    next(action);
    
    // Show loading indicator if a label is provided
    if (label) {
      store.dispatch(uiActions.setLoading(true));
    }
    
    try {
      // Make the API request
      const response = await axiosInstance({
        url,
        method,
        data,
        ...config,
      });
      
      // Dispatch success action
      const successAction = apiSuccess(response, apiAction.payload);
      store.dispatch(successAction);
      
      // Handle onSuccess action if provided
      if (onSuccess) {
        store.dispatch({
          type: onSuccess,
          payload: response.data,
          meta: { originalRequest: apiAction.payload },
        });
      }
      
      // Hydrate token if it's a login response
      if (response.data?.token) {
        localStorage.setItem('smartinsight_token', response.data.token);
      }
      
      return response;
    } catch (error) {
      // Dispatch failure action
      const failureAction = apiFailure(error, apiAction.payload);
      store.dispatch(failureAction);
      
      // Handle onFailure action if provided
      if (onFailure) {
        store.dispatch({
          type: onFailure,
          payload: error,
          meta: { originalRequest: apiAction.payload },
        });
      }
      
      // Handle 401 errors (unauthorized)
      if (isAxiosError(error) && error.response?.status === 401) {
        store.dispatch(authActions.logout());
        store.dispatch(uiActions.addNotification({
          message: 'Your session has expired. Please log in again.',
          type: 'warning',
        }));
      } else {
        // Show error notification
        const errorMessage = isAxiosError(error) && error.response?.data?.message
          ? error.response.data.message
          : 'An unexpected error occurred. Please try again.';
          
        store.dispatch(uiActions.addNotification({
          message: errorMessage,
          type: 'error',
        }));
      }
      
      throw error;
    } finally {
      // Hide loading indicator if a label is provided
      if (label) {
        store.dispatch(uiActions.setLoading(false));
      }
    }
  };
};

// Helper function to create API request actions
export const apiRequestHelper = (
  endpoint: string,
  method: 'get' | 'post' | 'put' | 'delete' | 'patch',
  data?: any,
  onSuccess?: string,
  onFailure?: string,
  headers?: Record<string, string>
): ApiRequestAction => ({
  type: API_REQUEST,
  payload: {
    url: endpoint,
    method,
    data,
    onSuccess,
    onFailure,
    config: {
      headers: {
        ...headers,
      },
    },
  },
}); 