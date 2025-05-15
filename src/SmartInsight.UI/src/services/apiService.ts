import { apiRequest } from '../store/middleware/apiMiddleware';
// import type { ApiRequestAction } from '../store/middleware/apiMiddleware';
import { AUTH_ACTIONS } from '../store/slices/authSlice';
import { DATA_ACTIONS } from '../store/slices/dataSlice';
import { CHAT_ACTIONS } from '../store/slices/chatSlice';
import type { AppAction } from '../store/StoreContext';
import type { Dispatch } from 'react';
import type { User } from '../store/slices/authSlice';

/**
 * Service for handling API requests through our state management
 */
export class ApiService {
  private dispatch: Dispatch<AppAction>;

  constructor(dispatch: Dispatch<AppAction>) {
    this.dispatch = dispatch;
  }

  // Auth API methods
  public login(username: string, password: string) {
    return this.dispatch(
      apiRequest(
        '/auth/login',
        'post',
        { username, password },
        AUTH_ACTIONS.LOGIN_SUCCESS,
        AUTH_ACTIONS.LOGIN_FAILURE,
        'login'
      )
    );
  }

  public logout() {
    return this.dispatch(
      apiRequest(
        '/auth/logout',
        'post',
        undefined,
        AUTH_ACTIONS.LOGOUT,
        undefined,
        'logout'
      )
    );
  }

  public refreshToken() {
    return this.dispatch(
      apiRequest(
        '/auth/refresh',
        'post',
        undefined,
        AUTH_ACTIONS.REFRESH_TOKEN,
        AUTH_ACTIONS.LOGIN_FAILURE,
        'refresh'
      )
    );
  }

  public register(user: Omit<User, 'id' | 'roles'> & { password: string }) {
    return this.dispatch(
      apiRequest(
        '/auth/register',
        'post',
        user,
        undefined,
        undefined,
        'register'
      )
    );
  }

  // Data Sources API methods
  public fetchDataSources() {
    return this.dispatch(
      apiRequest(
        '/data-sources',
        'get',
        undefined,
        DATA_ACTIONS.FETCH_DATA_SOURCES_SUCCESS,
        DATA_ACTIONS.FETCH_DATA_SOURCES_FAILURE,
        'fetchDataSources'
      )
    );
  }

  public createDataSource(dataSource: any) {
    return this.dispatch(
      apiRequest(
        '/data-sources',
        'post',
        dataSource,
        undefined,
        undefined,
        'createDataSource'
      )
    );
  }

  public updateDataSource(id: string, dataSource: any) {
    return this.dispatch(
      apiRequest(
        `/data-sources/${id}`,
        'put',
        dataSource,
        undefined,
        undefined,
        'updateDataSource'
      )
    );
  }

  public deleteDataSource(id: string) {
    return this.dispatch(
      apiRequest(
        `/data-sources/${id}`,
        'delete',
        undefined,
        undefined,
        undefined,
        'deleteDataSource'
      )
    );
  }

  // Datasets API methods
  public fetchDatasets() {
    return this.dispatch(
      apiRequest(
        '/datasets',
        'get',
        undefined,
        DATA_ACTIONS.FETCH_DATASETS_SUCCESS,
        DATA_ACTIONS.FETCH_DATASETS_FAILURE,
        'fetchDatasets'
      )
    );
  }

  public createDataset(dataset: any) {
    return this.dispatch(
      apiRequest(
        '/datasets',
        'post',
        dataset,
        undefined,
        undefined,
        'createDataset'
      )
    );
  }

  public updateDataset(id: string, dataset: any) {
    return this.dispatch(
      apiRequest(
        `/datasets/${id}`,
        'put',
        dataset,
        undefined,
        undefined,
        'updateDataset'
      )
    );
  }

  public deleteDataset(id: string) {
    return this.dispatch(
      apiRequest(
        `/datasets/${id}`,
        'delete',
        undefined,
        undefined,
        undefined,
        'deleteDataset'
      )
    );
  }

  // Chat API methods
  public fetchConversations() {
    return this.dispatch(
      apiRequest(
        '/chat/conversations',
        'get',
        undefined,
        CHAT_ACTIONS.FETCH_CONVERSATIONS_SUCCESS,
        CHAT_ACTIONS.FETCH_CONVERSATIONS_FAILURE,
        'fetchConversations'
      )
    );
  }

  public sendMessage(conversationId: string, message: string) {
    return this.dispatch(
      apiRequest(
        `/chat/conversations/${conversationId}/messages`,
        'post',
        { content: message },
        undefined,
        undefined,
        'sendMessage'
      )
    );
  }

  public createConversation(title: string) {
    return this.dispatch(
      apiRequest(
        '/chat/conversations',
        'post',
        { title },
        undefined,
        undefined,
        'createConversation'
      )
    );
  }

  public deleteConversation(id: string) {
    return this.dispatch(
      apiRequest(
        `/chat/conversations/${id}`,
        'delete',
        undefined,
        undefined,
        undefined,
        'deleteConversation'
      )
    );
  }
}

/**
 * Hook for using the API service with the current dispatch
 */
export function useApiService(dispatch: Dispatch<AppAction>) {
  return new ApiService(dispatch);
} 