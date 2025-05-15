import type { AppState, AppAction } from '../StoreContext';

// Types for the middleware
export type Dispatch<A> = (action: A) => void;
export type MiddlewareAPI = {
  getState: () => AppState;
  dispatch: Dispatch<AppAction>;
};
export type Middleware = (api: MiddlewareAPI) => (next: Dispatch<AppAction>) => (action: AppAction) => void;

// Define which slices of state should be persisted
const PERSISTED_KEYS: (keyof AppState)[] = ['auth', 'ui'];

// Storage keys
const STORAGE_KEY_PREFIX = 'smartinsight';

/**
 * Middleware to persist state to localStorage
 */
export const storageMiddleware: Middleware = (api) => (next) => (action) => {
  // Call the next dispatch method in the middleware chain
  next(action);

  // Get the current state after the action has been processed
  const state = api.getState();

  // Save the specified slices of state to localStorage
  PERSISTED_KEYS.forEach((key) => {
    try {
      const serializedState = JSON.stringify(state[key]);
      localStorage.setItem(`${STORAGE_KEY_PREFIX}_${key}`, serializedState);
    } catch (err) {
      console.error(`Could not save state for ${key}:`, err);
    }
  });
};

/**
 * Load persisted state from localStorage
 */
export function loadState(): Partial<AppState> {
  const persistedState: Partial<AppState> = {};

  PERSISTED_KEYS.forEach((key) => {
    try {
      const serializedState = localStorage.getItem(`${STORAGE_KEY_PREFIX}_${key}`);
      if (serializedState !== null) {
        persistedState[key] = JSON.parse(serializedState);
      }
    } catch (err) {
      console.error(`Could not load state for ${key}:`, err);
    }
  });

  return persistedState;
}

/**
 * Clear persisted state from localStorage
 */
export function clearPersistedState(): void {
  PERSISTED_KEYS.forEach((key) => {
    try {
      localStorage.removeItem(`${STORAGE_KEY_PREFIX}_${key}`);
    } catch (err) {
      console.error(`Could not clear state for ${key}:`, err);
    }
  });
} 