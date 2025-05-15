// Define the auth state type
export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  loading: boolean;
  error: string | null;
  tenantId: string | null;
}

// Define user type
export interface User {
  id: string;
  username: string;
  email: string;
  roles: string[];
}

// Define action types
export const AUTH_ACTIONS = {
  LOGIN_REQUEST: 'auth/loginRequest',
  LOGIN_SUCCESS: 'auth/loginSuccess',
  LOGIN_FAILURE: 'auth/loginFailure',
  LOGOUT: 'auth/logout',
  REFRESH_TOKEN: 'auth/refreshToken',
  SET_TENANT: 'auth/setTenant',
  CLEAR_ERROR: 'auth/clearError',
} as const;

// Define action interfaces
export type AuthAction =
  | { type: typeof AUTH_ACTIONS.LOGIN_REQUEST }
  | { type: typeof AUTH_ACTIONS.LOGIN_SUCCESS; payload: { user: User; token: string } }
  | { type: typeof AUTH_ACTIONS.LOGIN_FAILURE; payload: string }
  | { type: typeof AUTH_ACTIONS.LOGOUT }
  | { type: typeof AUTH_ACTIONS.REFRESH_TOKEN; payload: string }
  | { type: typeof AUTH_ACTIONS.SET_TENANT; payload: string }
  | { type: typeof AUTH_ACTIONS.CLEAR_ERROR };

// Initial state
const initialState: AuthState = {
  isAuthenticated: false,
  user: null,
  token: null,
  loading: false,
  error: null,
  tenantId: null,
};

// Create the reducer
export function authReducer(state: AuthState = initialState, action: AuthAction): AuthState {
  switch (action.type) {
    case AUTH_ACTIONS.LOGIN_REQUEST:
      return {
        ...state,
        loading: true,
        error: null,
      };
    case AUTH_ACTIONS.LOGIN_SUCCESS:
      return {
        ...state,
        isAuthenticated: true,
        user: action.payload.user,
        token: action.payload.token,
        loading: false,
        error: null,
      };
    case AUTH_ACTIONS.LOGIN_FAILURE:
      return {
        ...state,
        isAuthenticated: false,
        user: null,
        token: null,
        loading: false,
        error: action.payload,
      };
    case AUTH_ACTIONS.LOGOUT:
      return {
        ...initialState,
      };
    case AUTH_ACTIONS.REFRESH_TOKEN:
      return {
        ...state,
        token: action.payload,
      };
    case AUTH_ACTIONS.SET_TENANT:
      return {
        ...state,
        tenantId: action.payload,
      };
    case AUTH_ACTIONS.CLEAR_ERROR:
      return {
        ...state,
        error: null,
      };
    default:
      return state;
  }
}

// Action creators
export const authActions = {
  loginRequest: (): AuthAction => ({
    type: AUTH_ACTIONS.LOGIN_REQUEST,
  }),
  loginSuccess: (user: User, token: string): AuthAction => ({
    type: AUTH_ACTIONS.LOGIN_SUCCESS,
    payload: { user, token },
  }),
  loginFailure: (error: string): AuthAction => ({
    type: AUTH_ACTIONS.LOGIN_FAILURE,
    payload: error,
  }),
  logout: (): AuthAction => ({
    type: AUTH_ACTIONS.LOGOUT,
  }),
  refreshToken: (token: string): AuthAction => ({
    type: AUTH_ACTIONS.REFRESH_TOKEN,
    payload: token,
  }),
  setTenant: (tenantId: string): AuthAction => ({
    type: AUTH_ACTIONS.SET_TENANT,
    payload: tenantId,
  }),
  clearError: (): AuthAction => ({
    type: AUTH_ACTIONS.CLEAR_ERROR,
  }),
}; 