import { useSelector, useDispatch } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import type { RootState } from '../store/configureStore';
import { authActions } from '../store/slices/authSlice';
import authService from '../services/authService';

/**
 * Custom hook that provides authentication-related functionality
 */
export const useAuth = () => {
  const auth = useSelector((state: RootState) => state.auth);
  const dispatch = useDispatch();
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
  const hasRole = (role: string | string[]) => {
    if (!auth.user || !auth.user.roles) return false;
    
    if (Array.isArray(role)) {
      return role.some(r => auth.user?.roles.includes(r));
    }
    
    return auth.user.roles.includes(role);
  };
  
  /**
   * Check if the current user has any of the given roles
   */
  const hasAnyRole = (roles: string[]) => {
    return roles.some(role => hasRole(role));
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
    token: auth.token,
    tenantId: auth.tenantId,
    hasRole,
    hasAnyRole,
    setTenant,
    login,
    logout
  };
};

export default useAuth; 