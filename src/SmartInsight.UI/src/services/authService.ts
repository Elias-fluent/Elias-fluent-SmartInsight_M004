import axios from 'axios';
import { jwtDecode } from 'jwt-decode';

const API_URL = import.meta.env.VITE_API_URL || '/api';
const TOKEN_KEY = 'smartinsight_token';
const REFRESH_TOKEN_KEY = 'smartinsight_refresh_token';

interface JwtPayload {
  exp: number;
  sub: string;
  role: string[];
  email: string;
  tenantId?: string;
  username: string;
  nbf: number;
  iat: number;
  jti: string;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    username: string;
    email: string;
    roles: string[];
  };
}

interface LoginCredentials {
  usernameOrEmail: string;
  password: string;
  rememberMe?: boolean;
}

interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

/**
 * Service for handling authentication operations 
 */
class AuthService {
  /**
   * Attempts to login with the provided credentials
   */
  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    try {
      const response = await axios.post(`${API_URL}/api/v1/auth/login`, credentials);
      
      if (response.data.token) {
        this.setToken(response.data.token);
        this.setRefreshToken(response.data.refreshToken);
      }
      
      return response.data;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  }

  /**
   * Logs the user out by removing tokens
   */
  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  /**
   * Refreshes the access token using the refresh token
   */
  async refreshToken(): Promise<string> {
    const currentToken = this.getToken();
    const refreshToken = this.getRefreshToken();
    
    if (!currentToken || !refreshToken) {
      throw new Error('No tokens available for refresh');
    }
    
    try {
      const response = await axios.post(`${API_URL}/api/v1/auth/refresh`, {
        accessToken: currentToken,
        refreshToken: refreshToken
      } as RefreshTokenRequest);
      
      if (response.data.token) {
        this.setToken(response.data.token);
        this.setRefreshToken(response.data.refreshToken);
        return response.data.token;
      }
      
      throw new Error('Failed to refresh token');
    } catch (error) {
      console.error('Refresh token error:', error);
      this.logout(); // Clear tokens on failure
      throw error;
    }
  }

  /**
   * Stores the JWT token in localStorage
   */
  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  /**
   * Retrieves the JWT token from localStorage
   */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  /**
   * Stores the refresh token in localStorage
   */
  setRefreshToken(token: string): void {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  }

  /**
   * Retrieves the refresh token from localStorage
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  /**
   * Checks if the user is authenticated
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const currentTime = Date.now() / 1000;
      
      // If token is expired, try to refresh it
      if (decoded.exp < currentTime) {
        // We'll handle token refresh in the interceptor
        return false;
      }
      
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * Returns the current user information from the token
   */
  getCurrentUser() {
    const token = this.getToken();
    if (!token) return null;
    
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      return {
        id: decoded.sub,
        username: decoded.username,
        email: decoded.email,
        roles: decoded.role || [],
        tenantId: decoded.tenantId
      };
    } catch (error) {
      return null;
    }
  }

  /**
   * Checks if the current user has the specified role
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    
    return user.roles.includes(role);
  }

  /**
   * Gets the current tenant ID
   */
  getCurrentTenantId(): string | undefined {
    const user = this.getCurrentUser();
    return user?.tenantId;
  }

  /**
   * Sets up axios interceptors for authentication
   */
  setupInterceptors(): void {
    // Add request interceptor
    axios.interceptors.request.use(
      (config) => {
        const token = this.getToken();
        if (token) {
          config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Add response interceptor to handle token refresh
    axios.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;
        
        // If the error is 401 and we haven't already tried to refresh
        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;
          
          try {
            // Attempt to refresh the token
            const token = await this.refreshToken();
            
            // Update the request and retry
            originalRequest.headers['Authorization'] = `Bearer ${token}`;
            return axios(originalRequest);
          } catch (refreshError) {
            // Refresh failed, redirect to login
            this.logout();
            window.location.href = '/login';
            return Promise.reject(refreshError);
          }
        }
        
        return Promise.reject(error);
      }
    );
  }
}

// Create a singleton instance
const authService = new AuthService();
export default authService; 