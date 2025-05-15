type Reducer<S, A> = (state: S | undefined, action: A) => S;

type ReducersMapObject<S, A> = {
  [K in keyof S]: Reducer<S[K], A>;
};

/**
 * Combines multiple reducers into a single reducer function
 * Similar to Redux's combineReducers but with TypeScript support
 */
export function combineReducers<S, A>(reducers: ReducersMapObject<S, A>): Reducer<S, A> {
  return (state: S | undefined = {} as S, action: A): S => {
    const nextState: Partial<S> = {};
    let hasChanged = false;

    // Process each reducer with its own portion of the state
    for (const key in reducers) {
      if (Object.prototype.hasOwnProperty.call(reducers, key)) {
        const reducer = reducers[key];
        const previousStateForKey = state[key];
        const nextStateForKey = reducer(previousStateForKey, action);

        nextState[key] = nextStateForKey;
        hasChanged = hasChanged || nextStateForKey !== previousStateForKey;
      }
    }

    return hasChanged ? (nextState as S) : state;
  };
} 