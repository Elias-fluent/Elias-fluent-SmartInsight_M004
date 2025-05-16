import { useState, useCallback, useEffect, useRef } from 'react';

interface UseFocusVisibleOptions {
  // If true, will apply focus-visible class to the document body when in keyboard mode
  applyGlobalClass?: boolean;
  // If true, will default to keyboard navigation mode (useful for testing)
  defaultKeyboardMode?: boolean;
}

/**
 * Hook that helps differentiate between mouse and keyboard focus
 * to show focus outlines only during keyboard navigation
 * 
 * @param options - Configuration options
 * @returns Object with keyboard mode state and handler functions
 * 
 * @example
 * ```tsx
 * const MyButton = () => {
 *   const { isFocusVisible, focusProps } = useFocusVisible();
 *   
 *   return (
 *     <button 
 *       className={`btn ${isFocusVisible ? 'focus-visible' : ''}`}
 *       {...focusProps}
 *     >
 *       Click Me
 *     </button>
 *   );
 * }
 * ```
 */
export function useFocusVisible(options: UseFocusVisibleOptions = {}) {
  const { 
    applyGlobalClass = true,
    defaultKeyboardMode = false 
  } = options;
  
  // Track if we're in keyboard navigation mode
  const [isKeyboardMode, setIsKeyboardMode] = useState(defaultKeyboardMode);
  
  // Track which element has focus
  const [focusedElement, setFocusedElement] = useState<Element | null>(null);
  
  // Is focus visible for the current element?
  const isFocusVisible = isKeyboardMode && document.activeElement === focusedElement;
  
  // Last interaction was a keyboard tab
  const lastInteractionWasKeyboardRef = useRef(defaultKeyboardMode);
  
  // Switch to keyboard mode when Tab is used
  const handleKeyDown = useCallback((event: KeyboardEvent) => {
    // Tab key indicates keyboard navigation
    if (event.key === 'Tab') {
      lastInteractionWasKeyboardRef.current = true;
      setIsKeyboardMode(true);
    }
  }, []);
  
  // Switch away from keyboard mode on mouse/touch interaction
  const handlePointerEvent = useCallback(() => {
    lastInteractionWasKeyboardRef.current = false;
    setIsKeyboardMode(false);
  }, []);
  
  // Track focus events
  const handleFocus = useCallback((event: FocusEvent) => {
    // Show focus styles if last interaction was keyboard
    setFocusedElement(event.target as Element);
  }, []);
  
  // Track blur events
  const handleBlur = useCallback(() => {
    setFocusedElement(null);
  }, []);
  
  // Set up global event listeners for keyboard/mouse detection
  useEffect(() => {
    // Listen for Tab key to detect keyboard navigation
    document.addEventListener('keydown', handleKeyDown, true);
    
    // Listen for mouse/touch to detect pointer interaction
    document.addEventListener('mousedown', handlePointerEvent, true);
    document.addEventListener('touchstart', handlePointerEvent, true);
    
    // Remove listeners on cleanup
    return () => {
      document.removeEventListener('keydown', handleKeyDown, true);
      document.removeEventListener('mousedown', handlePointerEvent, true);
      document.removeEventListener('touchstart', handlePointerEvent, true);
    };
  }, [handleKeyDown, handlePointerEvent]);
  
  // Apply a class to the document body when in keyboard mode
  useEffect(() => {
    if (applyGlobalClass) {
      const className = 'keyboard-navigation-active';
      
      if (isKeyboardMode) {
        document.body.classList.add(className);
      } else {
        document.body.classList.remove(className);
      }
    }
  }, [isKeyboardMode, applyGlobalClass]);
  
  // Props to spread onto elements
  const focusProps = {
    onFocus: handleFocus,
    onBlur: handleBlur,
  };
  
  return {
    isFocusVisible,
    isKeyboardMode,
    focusProps,
  };
} 