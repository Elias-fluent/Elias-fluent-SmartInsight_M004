// Define the UI state type
export interface UIState {
  theme: 'light' | 'dark' | 'system';
  contrastMode: 'normal' | 'high';
  colorBlindMode: 'normal' | 'deuteranopia' | 'protanopia' | 'tritanopia';
  textSize: 'normal' | 'large' | 'x-large';
  sidebarOpen: boolean;
  notifications: Notification[];
  isLoading: boolean;
  activeModal: string | null;
  currentView: string;
}

// Define notification type
export interface Notification {
  id: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: number;
  read: boolean;
}

// Define action types
export const UI_ACTIONS = {
  SET_THEME: 'ui/setTheme',
  SET_CONTRAST_MODE: 'ui/setContrastMode',
  SET_COLOR_BLIND_MODE: 'ui/setColorBlindMode',
  SET_TEXT_SIZE: 'ui/setTextSize',
  TOGGLE_SIDEBAR: 'ui/toggleSidebar',
  SET_SIDEBAR: 'ui/setSidebar',
  ADD_NOTIFICATION: 'ui/addNotification',
  REMOVE_NOTIFICATION: 'ui/removeNotification',
  MARK_NOTIFICATION_READ: 'ui/markNotificationRead',
  CLEAR_NOTIFICATIONS: 'ui/clearNotifications',
  SET_LOADING: 'ui/setLoading',
  SET_MODAL: 'ui/setModal',
  SET_VIEW: 'ui/setView',
} as const;

// Define action interfaces
export type UIAction =
  | { type: typeof UI_ACTIONS.SET_THEME; payload: UIState['theme'] }
  | { type: typeof UI_ACTIONS.SET_CONTRAST_MODE; payload: UIState['contrastMode'] }
  | { type: typeof UI_ACTIONS.SET_COLOR_BLIND_MODE; payload: UIState['colorBlindMode'] }
  | { type: typeof UI_ACTIONS.SET_TEXT_SIZE; payload: UIState['textSize'] }
  | { type: typeof UI_ACTIONS.TOGGLE_SIDEBAR }
  | { type: typeof UI_ACTIONS.SET_SIDEBAR; payload: boolean }
  | { type: typeof UI_ACTIONS.ADD_NOTIFICATION; payload: Omit<Notification, 'id' | 'timestamp' | 'read'> }
  | { type: typeof UI_ACTIONS.REMOVE_NOTIFICATION; payload: string }
  | { type: typeof UI_ACTIONS.MARK_NOTIFICATION_READ; payload: string }
  | { type: typeof UI_ACTIONS.CLEAR_NOTIFICATIONS }
  | { type: typeof UI_ACTIONS.SET_LOADING; payload: boolean }
  | { type: typeof UI_ACTIONS.SET_MODAL; payload: string | null }
  | { type: typeof UI_ACTIONS.SET_VIEW; payload: string };

// Initial state
const initialState: UIState = {
  theme: 'system',
  contrastMode: 'normal',
  colorBlindMode: 'normal',
  textSize: 'normal',
  sidebarOpen: true,
  notifications: [],
  isLoading: false,
  activeModal: null,
  currentView: 'dashboard',
};

// Create the reducer
export function uiReducer(state: UIState = initialState, action: UIAction): UIState {
  switch (action.type) {
    case UI_ACTIONS.SET_THEME:
      return {
        ...state,
        theme: action.payload,
      };
    case UI_ACTIONS.SET_CONTRAST_MODE:
      return {
        ...state,
        contrastMode: action.payload,
      };
    case UI_ACTIONS.SET_COLOR_BLIND_MODE:
      return {
        ...state,
        colorBlindMode: action.payload,
      };
    case UI_ACTIONS.SET_TEXT_SIZE:
      return {
        ...state,
        textSize: action.payload,
      };
    case UI_ACTIONS.TOGGLE_SIDEBAR:
      return {
        ...state,
        sidebarOpen: !state.sidebarOpen,
      };
    case UI_ACTIONS.SET_SIDEBAR:
      return {
        ...state,
        sidebarOpen: action.payload,
      };
    case UI_ACTIONS.ADD_NOTIFICATION:
      return {
        ...state,
        notifications: [
          {
            id: crypto.randomUUID(),
            timestamp: Date.now(),
            read: false,
            ...action.payload,
          },
          ...state.notifications,
        ].slice(0, 100), // Limit to 100 notifications
      };
    case UI_ACTIONS.REMOVE_NOTIFICATION:
      return {
        ...state,
        notifications: state.notifications.filter(
          (notification) => notification.id !== action.payload
        ),
      };
    case UI_ACTIONS.MARK_NOTIFICATION_READ:
      return {
        ...state,
        notifications: state.notifications.map((notification) =>
          notification.id === action.payload
            ? { ...notification, read: true }
            : notification
        ),
      };
    case UI_ACTIONS.CLEAR_NOTIFICATIONS:
      return {
        ...state,
        notifications: [],
      };
    case UI_ACTIONS.SET_LOADING:
      return {
        ...state,
        isLoading: action.payload,
      };
    case UI_ACTIONS.SET_MODAL:
      return {
        ...state,
        activeModal: action.payload,
      };
    case UI_ACTIONS.SET_VIEW:
      return {
        ...state,
        currentView: action.payload,
      };
    default:
      return state;
  }
}

// Action creators
export const uiActions = {
  setTheme: (theme: UIState['theme']): UIAction => ({
    type: UI_ACTIONS.SET_THEME,
    payload: theme,
  }),
  setContrastMode: (mode: UIState['contrastMode']): UIAction => ({
    type: UI_ACTIONS.SET_CONTRAST_MODE,
    payload: mode,
  }),
  setColorBlindMode: (mode: UIState['colorBlindMode']): UIAction => ({
    type: UI_ACTIONS.SET_COLOR_BLIND_MODE,
    payload: mode,
  }),
  setTextSize: (size: UIState['textSize']): UIAction => ({
    type: UI_ACTIONS.SET_TEXT_SIZE,
    payload: size,
  }),
  toggleSidebar: (): UIAction => ({
    type: UI_ACTIONS.TOGGLE_SIDEBAR,
  }),
  setSidebar: (open: boolean): UIAction => ({
    type: UI_ACTIONS.SET_SIDEBAR,
    payload: open,
  }),
  addNotification: (
    notification: Omit<Notification, 'id' | 'timestamp' | 'read'>
  ): UIAction => ({
    type: UI_ACTIONS.ADD_NOTIFICATION,
    payload: notification,
  }),
  removeNotification: (id: string): UIAction => ({
    type: UI_ACTIONS.REMOVE_NOTIFICATION,
    payload: id,
  }),
  markNotificationRead: (id: string): UIAction => ({
    type: UI_ACTIONS.MARK_NOTIFICATION_READ,
    payload: id,
  }),
  clearNotifications: (): UIAction => ({
    type: UI_ACTIONS.CLEAR_NOTIFICATIONS,
  }),
  setLoading: (loading: boolean): UIAction => ({
    type: UI_ACTIONS.SET_LOADING,
    payload: loading,
  }),
  setModal: (modal: string | null): UIAction => ({
    type: UI_ACTIONS.SET_MODAL,
    payload: modal,
  }),
  setView: (view: string): UIAction => ({
    type: UI_ACTIONS.SET_VIEW,
    payload: view,
  }),
}; 