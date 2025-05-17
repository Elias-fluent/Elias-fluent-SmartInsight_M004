import React, { createContext, useContext, useReducer, useRef, useCallback } from 'react';
import type { ReactNode, Dispatch } from 'react';
import { combineReducers } from './utils/combineReducers';

// Import all reducers
import { authReducer } from './slices/authSlice';
import type { AuthState, AuthAction } from './slices/authSlice';
import { uiReducer } from './slices/uiSlice';
import type { UIState, UIAction } from './slices/uiSlice';
import dataReducer from './slices/dataSlice';
import type { DataState, DataAction } from './slices/dataSlice';
import { chatReducer } from './slices/chatSlice';
import type { ChatState, ChatAction } from './slices/chatSlice';

// Import middleware and API action types
import { middleware } from './middleware';
import { loadState } from './middleware/storageMiddleware';
import type { ApiAction } from './middleware/apiMiddleware';

// Define the shape of the combined state
export interface AppState {
  auth: AuthState;
  ui: UIState;
  data: DataState;
  chat: ChatState;
}

// Define the combined action types
export type AppAction = 
  | AuthAction 
  | UIAction 
  | DataAction 
  | ChatAction
  | ApiAction;

// Create a dummy init action that can be cast to any action type
const INIT_ACTION = { type: '@@INIT' };

// Combine all reducers into a single root reducer
const rootReducer = combineReducers<AppState, AppAction>({
  auth: authReducer as any, // Type assertion to work around type incompatibility
  ui: uiReducer as any, // Type assertion to work around type incompatibility
  data: dataReducer as any, // Type assertion to work around type incompatibility
  chat: chatReducer as any, // Type assertion to work around type incompatibility
});

// Load persisted state from localStorage
const persistedState = loadState();

// Create the initial state by calling each reducer with undefined state
// and merge with any persisted state
const initialState: AppState = {
  auth: {
    ...authReducer(undefined, INIT_ACTION as unknown as AuthAction),
    ...persistedState.auth,
  },
  ui: {
    ...uiReducer(undefined, INIT_ACTION as unknown as UIAction),
    ...persistedState.ui,
  },
  data: {
    ...dataReducer(undefined, INIT_ACTION as unknown as DataAction),
    ...persistedState.data,
  },
  chat: {
    ...chatReducer(undefined, INIT_ACTION as unknown as ChatAction),
    ...persistedState.chat,
  },
};

// Create the context
interface StoreContextType {
  state: AppState;
  dispatch: Dispatch<AppAction>;
}

const StoreContext = createContext<StoreContextType | undefined>(undefined);

// Provider component that wraps your app and makes store available to all components
interface StoreProviderProps {
  children: ReactNode;
}

export const StoreProvider: React.FC<StoreProviderProps> = ({ children }) => {
  // Create the reducer with initial state
  const [state, baseDispatch] = useReducer(rootReducer, initialState);
  
  // Create a mutable ref to hold the state for middleware
  const storeRef = useRef({
    getState: () => state,
    dispatch: (action: AppAction) => baseDispatch(action),
  });
  
  // Update the ref when state changes
  storeRef.current.getState = () => state;
  
  // Create a middleware-enhanced dispatch function
  const enhancedDispatch = useCallback(
    middleware(storeRef.current)(
      (action: AppAction) => baseDispatch(action)
    ),
    [baseDispatch]
  );
  
  // Update the ref's dispatch when enhancedDispatch changes
  storeRef.current.dispatch = enhancedDispatch;

  return (
    <StoreContext.Provider value={{ state, dispatch: enhancedDispatch }}>
      {children}
    </StoreContext.Provider>
  );
};

// Custom hook to use the store context
export const useStore = () => {
  const context = useContext(StoreContext);
  if (context === undefined) {
    throw new Error('useStore must be used within a StoreProvider');
  }
  return context;
};

// Selector hooks for specific slices
export const useAuth = () => {
  const { state, dispatch } = useStore();
  return {
    auth: state.auth,
    dispatch,
  };
};

export const useUI = () => {
  const { state, dispatch } = useStore();
  return {
    ui: state.ui,
    dispatch,
  };
};

export const useData = () => {
  const { state, dispatch } = useStore();
  return {
    data: state.data,
    dispatch,
  };
};

export const useChat = () => {
  const { state, dispatch } = useStore();
  return {
    chat: state.chat,
    dispatch,
  };
}; 