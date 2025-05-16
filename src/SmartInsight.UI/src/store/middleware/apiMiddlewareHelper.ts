import axios from 'axios';
import type { AxiosRequestConfig } from 'axios';
import type { Dispatch, Action } from 'redux';

// Helper function to make API requests through redux
export const apiRequest = (
  url: string,
  method: 'get' | 'post' | 'put' | 'delete' | 'patch',
  data?: any,
  onSuccess?: string,
  onFailure?: string,
  config?: AxiosRequestConfig
) => {
  // Return a thunk function instead of an action
  return async (dispatch: Dispatch<Action>) => {
    try {
      // Make API request
      const response = await axios({
        url,
        method,
        data,
        ...config,
      });

      // Dispatch success action if specified
      if (onSuccess) {
        dispatch({
          type: onSuccess,
          payload: response.data,
        });
      }

      // Return successful response for immediate use
      return { success: true, payload: response.data };
    } catch (error: any) {
      // Get error details
      const errorMessage = error.response?.data?.message || error.message || 'Unknown error';

      // Dispatch failure action if specified
      if (onFailure) {
        dispatch({
          type: onFailure,
          payload: errorMessage,
          error: true,
        });
      }

      // Return error for immediate handling
      return { success: false, payload: { errorMessage } };
    }
  };
}; 