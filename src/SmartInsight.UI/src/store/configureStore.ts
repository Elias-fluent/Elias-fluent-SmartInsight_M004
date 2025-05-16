import { combineReducers } from 'redux';
import { authReducer } from './slices/authSlice';
import { uiReducer } from './slices/uiSlice';
import { dataReducer } from './slices/dataSlice';
import { chatReducer } from './slices/chatSlice';
import type { AuthState } from './slices/authSlice';
import type { UIState } from './slices/uiSlice';
import type { DataState } from './slices/dataSlice';
import type { ChatState } from './slices/chatSlice';

// Define the shape of the combined state
export interface RootState {
  auth: AuthState;
  ui: UIState;
  data: DataState;
  chat: ChatState;
}

// Combine all reducers
export const rootReducer = combineReducers({
  auth: authReducer,
  ui: uiReducer,
  data: dataReducer,
  chat: chatReducer,
});

// Initial state
export const initialState: Partial<RootState> = {
  auth: {
    isAuthenticated: false,
    user: null,
    token: null,
    loading: false,
    error: null,
    tenantId: null,
  },
  ui: {
    isLoading: false,
    notifications: [],
    theme: 'light',
    sidebarOpen: true,
    activeModal: null,
    currentView: 'dashboard',
    contrastMode: 'normal',
    colorBlindMode: 'normal',
    textSize: 'normal',
  },
  data: {
    dataSources: [],
    datasets: [],
    queries: [],
    visualizations: [],
    selectedDataSource: null,
    selectedDataset: null,
    isLoading: false,
    error: null,
    lastUpdated: null,
  },
  chat: {
    conversations: [],
    activeConversationId: null,
    isLoading: false,
    error: null,
  },
};

// Export store type
export type AppDispatch = typeof store.dispatch;

// Create the store with middleware
const store = {
  dispatch: (() => {}) as any,
};

export default store; 