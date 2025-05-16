import { useEffect, useState } from 'react';
import { useUI } from '../store/StoreContext';
import { uiActions } from '../store/slices/uiSlice';

type Theme = 'light' | 'dark' | 'system';
type ContrastMode = 'normal' | 'high';
type ColorBlindMode = 'normal' | 'deuteranopia' | 'protanopia' | 'tritanopia';
type TextSize = 'normal' | 'large' | 'x-large';

/**
 * Hook for managing theme state with automatic system theme detection and accessibility features
 * 
 * @returns {Object} Theme and accessibility management object
 * @property {Theme} theme - Current theme ('light', 'dark', or 'system')
 * @property {boolean} isDark - Whether the current effective theme is dark
 * @property {ContrastMode} contrastMode - Current contrast mode ('normal' or 'high')
 * @property {ColorBlindMode} colorBlindMode - Current color blind mode
 * @property {TextSize} textSize - Current text size preference
 * @property {function} setTheme - Function to change the theme
 * @property {function} toggleTheme - Function to toggle between light and dark
 * @property {function} setContrastMode - Function to set contrast mode
 * @property {function} toggleContrastMode - Function to toggle between normal and high contrast
 * @property {function} setColorBlindMode - Function to set color blind mode
 * @property {function} setTextSize - Function to set text size
 */
export function useTheme() {
  const { ui, dispatch } = useUI();
  const [systemTheme, setSystemTheme] = useState<'light' | 'dark'>(
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  );
  const [prefersReducedMotion, setPrefersReducedMotion] = useState(
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
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

  // Set contrast mode
  const setContrastMode = (mode: ContrastMode) => {
    dispatch(uiActions.setContrastMode(mode));
  };

  // Toggle between normal and high contrast
  const toggleContrastMode = () => {
    dispatch(uiActions.setContrastMode(ui.contrastMode === 'normal' ? 'high' : 'normal'));
  };

  // Set color blind mode
  const setColorBlindMode = (mode: ColorBlindMode) => {
    dispatch(uiActions.setColorBlindMode(mode));
  };

  // Set text size
  const setTextSize = (size: TextSize) => {
    dispatch(uiActions.setTextSize(size));
  };

  // Listen for system theme changes
  useEffect(() => {
    const themeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const motionMediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    
    const handleThemeChange = (e: MediaQueryListEvent) => {
      setSystemTheme(e.matches ? 'dark' : 'light');
    };

    const handleMotionChange = (e: MediaQueryListEvent) => {
      setPrefersReducedMotion(e.matches);
    };
    
    // Add listeners
    themeMediaQuery.addEventListener('change', handleThemeChange);
    motionMediaQuery.addEventListener('change', handleMotionChange);
    
    // Clean up
    return () => {
      themeMediaQuery.removeEventListener('change', handleThemeChange);
      motionMediaQuery.removeEventListener('change', handleMotionChange);
    };
  }, []);

  // Apply theme and accessibility settings to document
  useEffect(() => {
    // Remove all theme and contrast classes
    document.documentElement.classList.remove(
      'light', 'dark',
      'contrast-normal', 'contrast-high',
      'colorblind-normal', 'colorblind-deuteranopia', 'colorblind-protanopia', 'colorblind-tritanopia',
      'text-normal', 'text-large', 'text-x-large'
    );
    
    // Add current theme class
    document.documentElement.classList.add(isDark ? 'dark' : 'light');
    
    // Add contrast mode class
    document.documentElement.classList.add(`contrast-${ui.contrastMode}`);
    
    // Add color blind mode class
    document.documentElement.classList.add(`colorblind-${ui.colorBlindMode}`);
    
    // Add text size class
    document.documentElement.classList.add(`text-${ui.textSize}`);

    // Add reduced motion class if system preference is set
    if (prefersReducedMotion) {
      document.documentElement.classList.add('reduced-motion');
    } else {
      document.documentElement.classList.remove('reduced-motion');
    }
  }, [isDark, ui.contrastMode, ui.colorBlindMode, ui.textSize, prefersReducedMotion]);

  return {
    theme: ui.theme,
    isDark,
    contrastMode: ui.contrastMode,
    colorBlindMode: ui.colorBlindMode,
    textSize: ui.textSize,
    prefersReducedMotion,
    setTheme,
    toggleTheme,
    setContrastMode,
    toggleContrastMode,
    setColorBlindMode,
    setTextSize,
  };
} 