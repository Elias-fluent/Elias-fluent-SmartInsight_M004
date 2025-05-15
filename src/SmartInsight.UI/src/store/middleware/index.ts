import type { Dispatch } from 'react';
import type { AppAction, AppState } from '../StoreContext';
import type { Middleware } from './storageMiddleware';
import { storageMiddleware } from './storageMiddleware';
import { apiMiddleware } from './apiMiddleware';

// Define a middleware orchestrator
export type MiddlewareOrchestrator = (
  store: { getState: () => AppState; dispatch: Dispatch<AppAction> }
) => (next: Dispatch<AppAction>) => (action: AppAction) => void;

// Combine multiple middleware into one
export const composeMiddleware = (...middlewares: Middleware[]): MiddlewareOrchestrator => {
  return (store) => {
    // Apply each middleware to the store
    const chain = middlewares.map(middleware => middleware(store));
    
    // Return a function that processes an action through all middleware
    return (next) => {
      // Create a chain of middlewares
      const dispatch = chain.reduceRight(
        (nextDispatch, middleware) => middleware(nextDispatch),
        next
      );
      
      // Return the final dispatch function
      return (action) => dispatch(action);
    };
  };
};

// Create the middleware chain
export const middleware = composeMiddleware(
  apiMiddleware,
  storageMiddleware,
  // Add additional middleware here
);

// Export individual middleware for direct usage
export { storageMiddleware, apiMiddleware }; 