import { useMemo } from 'react';
import { useStore } from '../store/StoreContext';
import { ApiService } from '../services/apiService';

/**
 * Hook to access the API service with the current store dispatch
 * Returns a memoized instance of the API service that can be used
 * to make API calls that integrate with the state management
 * 
 * @example
 * const api = useApi();
 * 
 * // In a component
 * const handleLogin = async () => {
 *   try {
 *     await api.login(username, password);
 *   } catch (error) {
 *     console.error('Login failed', error);
 *   }
 * };
 */
export function useApi() {
  const { dispatch } = useStore();
  
  // Memoize the API service to avoid unnecessary recreations
  const api = useMemo(() => new ApiService(dispatch), [dispatch]);
  
  return api;
} 