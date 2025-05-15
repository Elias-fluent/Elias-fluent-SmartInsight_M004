import { useEffect, useState } from 'react';
import { useUI } from '../store/StoreContext';
import { uiActions } from '../store/slices/uiSlice';

type Theme = 'light' | 'dark' | 'system';

/**
 * Hook for managing theme state with automatic system theme detection
 * 
 * @returns {Object} Theme management object
 * @property {Theme} theme - Current theme ('light', 'dark', or 'system')
 * @property {boolean} isDark - Whether the current effective theme is dark
 * @property {function} setTheme - Function to change the theme
 * @property {function} toggleTheme - Function to toggle between light and dark
 */
export function useTheme() {
  const { ui, dispatch } = useUI();
  const [systemTheme, setSystemTheme] = useState<'light' | 'dark'>(
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  );

  // Effective theme is either the selected theme or the system theme if set to 'system'
  const effectiveTheme = ui.theme === 'system' ? systemTheme : ui.theme;
  const isDark = effectiveTheme === 'dark';

  // Set theme and apply to document
  const setTheme = (theme: Theme) => {
    dispatch(uiActions.setTheme(theme));
  };

  // Toggle between light and dark (only if not using system)
  const toggleTheme = () => {
    if (ui.theme === 'system') {
      // If system, switch to the opposite of the current system theme
      setTheme(systemTheme === 'dark' ? 'light' : 'dark');
    } else {
      // Otherwise toggle the current explicit theme
      setTheme(ui.theme === 'dark' ? 'light' : 'dark');
    }
  };

  // Listen for system theme changes
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    const handleChange = (e: MediaQueryListEvent) => {
      setSystemTheme(e.matches ? 'dark' : 'light');
    };
    
    // Add listener
    mediaQuery.addEventListener('change', handleChange);
    
    // Clean up
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, []);

  // Apply theme to document
  useEffect(() => {
    document.documentElement.classList.remove('light', 'dark');
    document.documentElement.classList.add(isDark ? 'dark' : 'light');
  }, [isDark]);

  return {
    theme: ui.theme,
    isDark,
    setTheme,
    toggleTheme,
  };
} 