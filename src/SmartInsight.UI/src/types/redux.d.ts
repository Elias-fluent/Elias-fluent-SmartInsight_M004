// Type declarations to fix dispatch issues
import { ThunkDispatch } from 'redux-thunk';

// Extend the dispatch function to handle both regular actions and thunks
declare module 'react-redux' {
  export interface DefaultRootState {
    dataSources: {
      items: any[];
      loading: boolean;
      error: string | null;
    };
    // Add more state slices as needed
  }

  export type AppDispatch = ThunkDispatch<any, any, any>;

  export function useDispatch(): AppDispatch;
}

// Make TypeScript accept our api request thunks
declare module 'redux' {
  interface Dispatch<A extends Action = AnyAction> {
    <T extends A>(action: T): T;
    <R>(asyncAction: (dispatch: Dispatch<A>) => R): R;
  }
} 