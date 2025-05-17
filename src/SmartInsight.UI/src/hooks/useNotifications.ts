import { useCallback } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { uiActions } from '../store/slices/uiSlice';
import type { Notification } from '../store/slices/uiSlice';
import type { RootState } from '../store/configureStore';

/**
 * Hook for managing notifications
 * 
 * @returns {Object} Notification management functions
 * @property {function} showNotification - Function to show a new notification
 * @property {function} clearNotification - Function to remove a notification by id
 * @property {function} markAsRead - Function to mark a notification as read
 * @property {function} clearAll - Function to clear all notifications
 * @property {Notification[]} notifications - List of all current notifications
 */
export function useNotifications() {
  const ui = useSelector((state: RootState) => state.ui);
  const dispatch = useDispatch();
  
  // Show a notification
  const showNotification = useCallback(
    (message: string, type: Notification['type'] = 'info') => {
      dispatch(uiActions.addNotification({ message, type }));
    },
    [dispatch]
  );
  
  // Show a success notification
  const showSuccess = useCallback(
    (message: string) => {
      showNotification(message, 'success');
    },
    [showNotification]
  );
  
  // Show an error notification
  const showError = useCallback(
    (message: string) => {
      showNotification(message, 'error');
    },
    [showNotification]
  );
  
  // Show a warning notification
  const showWarning = useCallback(
    (message: string) => {
      showNotification(message, 'warning');
    },
    [showNotification]
  );
  
  // Clear a notification by id
  const clearNotification = useCallback(
    (id: string) => {
      dispatch(uiActions.removeNotification(id));
    },
    [dispatch]
  );
  
  // Mark a notification as read
  const markAsRead = useCallback(
    (id: string) => {
      dispatch(uiActions.markNotificationRead(id));
    },
    [dispatch]
  );
  
  // Clear all notifications
  const clearAll = useCallback(() => {
    dispatch(uiActions.clearNotifications());
  }, [dispatch]);
  
  return {
    showNotification,
    showSuccess,
    showError,
    showWarning,
    clearNotification,
    markAsRead,
    clearAll,
    notifications: ui.notifications,
  };
} 