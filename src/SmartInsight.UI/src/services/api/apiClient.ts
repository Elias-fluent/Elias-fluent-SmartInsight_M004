import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse, CancelTokenSource, InternalAxiosRequestConfig } from 'axios';

/**
 * Configuration options for the API client
 */
export interface ApiClientConfig {
  baseURL?: string;
  timeout?: number;
  headers?: Record<string, string>;
}

/**
 * Default API client configuration
 */
const defaultConfig: ApiClientConfig = {
  baseURL: import.meta.env.VITE_API_URL || '/api',
  timeout: 30000, // 30 seconds
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
};

/**
 * Class representing the core API client
 */
export class ApiClient {
  private axiosInstance: AxiosInstance;
  private cancelTokenSources: Map<string, CancelTokenSource> = new Map();

  /**
   * Creates an instance of ApiClient.
   * @param config - Configuration options for the API client
   */
  constructor(config: ApiClientConfig = {}) {
    // Merge default config with provided config
    const mergedConfig: AxiosRequestConfig = {
      ...defaultConfig,
      ...config,
      headers: {
        ...defaultConfig.headers,
        ...config.headers,
      },
    };

    // Create the axios instance with the merged config
    this.axiosInstance = axios.create(mergedConfig);
  }

  /**
   * Get the axios instance
   */
  getAxiosInstance(): AxiosInstance {
    return this.axiosInstance;
  }

  /**
   * Add a request interceptor
   */
  addRequestInterceptor(
    onFulfilled?: (config: InternalAxiosRequestConfig) => InternalAxiosRequestConfig | Promise<InternalAxiosRequestConfig>,
    onRejected?: (error: any) => any
  ): number {
    return this.axiosInstance.interceptors.request.use(onFulfilled, onRejected);
  }

  /**
   * Add a response interceptor
   */
  addResponseInterceptor(
    onFulfilled?: (response: AxiosResponse) => AxiosResponse | Promise<AxiosResponse>,
    onRejected?: (error: any) => any
  ): number {
    return this.axiosInstance.interceptors.response.use(onFulfilled, onRejected);
  }

  /**
   * Remove a request interceptor
   */
  removeRequestInterceptor(id: number): void {
    this.axiosInstance.interceptors.request.eject(id);
  }

  /**
   * Remove a response interceptor
   */
  removeResponseInterceptor(id: number): void {
    this.axiosInstance.interceptors.response.eject(id);
  }

  /**
   * Create a cancel token for the given request ID
   */
  createCancelToken(requestId: string): CancelTokenSource {
    // Cancel any existing request with the same ID
    if (this.cancelTokenSources.has(requestId)) {
      this.cancelRequest(requestId);
    }

    // Create a new cancel token source
    const cancelTokenSource = axios.CancelToken.source();
    this.cancelTokenSources.set(requestId, cancelTokenSource);
    return cancelTokenSource;
  }

  /**
   * Cancel a request with the given ID
   */
  cancelRequest(requestId: string, message = 'Request cancelled'): void {
    const source = this.cancelTokenSources.get(requestId);
    if (source) {
      source.cancel(message);
      this.cancelTokenSources.delete(requestId);
    }
  }

  /**
   * Cancel all pending requests
   */
  cancelAllRequests(message = 'All requests cancelled'): void {
    this.cancelTokenSources.forEach((source) => {
      source.cancel(message);
    });
    this.cancelTokenSources.clear();
  }

  /**
   * Make a GET request
   */
  async get<T = any>(
    url: string,
    config?: AxiosRequestConfig,
    requestId?: string
  ): Promise<AxiosResponse<T>> {
    const requestConfig = { ...config };
    
    if (requestId) {
      const cancelTokenSource = this.createCancelToken(requestId);
      requestConfig.cancelToken = cancelTokenSource.token;
    }
    
    return this.axiosInstance.get<T>(url, requestConfig);
  }

  /**
   * Make a POST request
   */
  async post<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig,
    requestId?: string
  ): Promise<AxiosResponse<T>> {
    const requestConfig = { ...config };
    
    if (requestId) {
      const cancelTokenSource = this.createCancelToken(requestId);
      requestConfig.cancelToken = cancelTokenSource.token;
    }
    
    return this.axiosInstance.post<T>(url, data, requestConfig);
  }

  /**
   * Make a PUT request
   */
  async put<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig,
    requestId?: string
  ): Promise<AxiosResponse<T>> {
    const requestConfig = { ...config };
    
    if (requestId) {
      const cancelTokenSource = this.createCancelToken(requestId);
      requestConfig.cancelToken = cancelTokenSource.token;
    }
    
    return this.axiosInstance.put<T>(url, data, requestConfig);
  }

  /**
   * Make a DELETE request
   */
  async delete<T = any>(
    url: string,
    config?: AxiosRequestConfig,
    requestId?: string
  ): Promise<AxiosResponse<T>> {
    const requestConfig = { ...config };
    
    if (requestId) {
      const cancelTokenSource = this.createCancelToken(requestId);
      requestConfig.cancelToken = cancelTokenSource.token;
    }
    
    return this.axiosInstance.delete<T>(url, requestConfig);
  }

  /**
   * Make a PATCH request
   */
  async patch<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig,
    requestId?: string
  ): Promise<AxiosResponse<T>> {
    const requestConfig = { ...config };
    
    if (requestId) {
      const cancelTokenSource = this.createCancelToken(requestId);
      requestConfig.cancelToken = cancelTokenSource.token;
    }
    
    return this.axiosInstance.patch<T>(url, data, requestConfig);
  }
}

// Create a default instance
export const apiClient = new ApiClient();
export default apiClient; 