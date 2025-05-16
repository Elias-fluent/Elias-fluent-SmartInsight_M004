import { useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import { authActions } from '../store/slices/authSlice';
import { useStore } from '../store/StoreContext';

/**
 * Custom hook that provides authentication-related functionality
 */
export const useAuth = () => {
  const { state, dispatch } = useStore();
  const auth = state.auth;
  const navigate = useNavigate();
  
  /**
   * Login the user with username/email and password
   */
  const login = async (usernameOrEmail: string, password: string, rememberMe = false) => {
    try {
      dispatch(authActions.loginRequest());
      const response = await authService.login({ usernameOrEmail, password, rememberMe });
      dispatch(authActions.loginSuccess(response.user, response.token));
      return response;
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Login failed. Please check your credentials.';
      dispatch(authActions.loginFailure(errorMessage));
      throw error;
    }
  };
  
  /**
   * Logout the current user
   */
  const logout = () => {
    authService.logout();
    dispatch(authActions.logout());
    navigate('/login');
  };
  
  /**
   * Check if the current user has a specific role
   */
  const hasRole = (role: string) => {
    return auth.user?.roles?.includes(role) || false;
  };
  
  /**
   * Check if the current user has any of the given roles
   */
  const hasAnyRole = (roles: string[]) => {
    if (!auth.user?.roles) return false;
    return roles.some(role => auth.user!.roles.includes(role));
  };
  
  /**
   * Set the current tenant ID
   */
  const setTenant = (tenantId: string) => {
    dispatch(authActions.setTenant(tenantId));
  };
  
  return {
    user: auth.user,
    isAuthenticated: auth.isAuthenticated,
    loading: auth.loading,
    error: auth.error,
    tenantId: auth.tenantId,
    login,
    logout,
    hasRole,
    hasAnyRole,
    setTenant
  };
};

export default useAuth; 