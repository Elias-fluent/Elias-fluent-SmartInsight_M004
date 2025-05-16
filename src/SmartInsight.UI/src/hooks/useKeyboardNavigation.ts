import { useState, useCallback, useEffect } from 'react';
import type { KeyboardEvent as ReactKeyboardEvent, RefObject } from 'react';

type Direction = 'horizontal' | 'vertical' | 'both';
type NavigationKey = 'ArrowUp' | 'ArrowDown' | 'ArrowLeft' | 'ArrowRight' | 'Home' | 'End' | 'Enter' | ' ';

interface UseKeyboardNavigationOptions {
  // Total number of items that can be navigated
  itemCount: number;
  // Initial focused index (default: -1, meaning no initial focus)
  defaultIndex?: number;
  // Which direction keys should be handled
  direction?: Direction;
  // Whether to wrap from last to first and vice versa
  wrap?: boolean;
  // Whether the navigation is currently enabled
  enabled?: boolean;
  // Callback when an item is selected (e.g., on Enter key)
  onSelect?: (index: number) => void;
  // Optional ref to the container for auto-focusing elements
  containerRef?: RefObject<HTMLElement>;
  // CSS selector for focusable items within the container
  itemSelector?: string;
}

/**
 * Hook for managing keyboard navigation within a collection of items
 * 
 * @param options - Configuration options
 * @returns Object with current index and event handlers
 * 
 * @example
 * ```tsx
 * const MyList = ({ items }) => {
 *   const listRef = useRef(null);
 *   const { focusedIndex, handleKeyDown } = useKeyboardNavigation({
 *     itemCount: items.length,
 *     direction: 'vertical',
 *     wrap: true,
 *     containerRef: listRef,
 *     onSelect: index => console.log(`Selected item ${index}`),
 *   });
 *   
 *   return (
 *     <ul ref={listRef} onKeyDown={handleKeyDown} role="listbox" tabIndex={0}>
 *       {items.map((item, index) => (
 *         <li 
 *           key={index}
 *           role="option"
 *           tabIndex={focusedIndex === index ? 0 : -1}
 *           aria-selected={focusedIndex === index}
 *         >
 *           {item.name}
 *         </li>
 *       ))}
 *     </ul>
 *   );
 * }
 * ```
 */
export function useKeyboardNavigation(options: UseKeyboardNavigationOptions) {
  const {
    itemCount,
    defaultIndex = -1,
    direction = 'vertical',
    wrap = false,
    enabled = true,
    onSelect,
    containerRef,
    itemSelector = '[role="option"], [role="menuitem"], [role="tab"], li, button',
  } = options;

  const [focusedIndex, setFocusedIndex] = useState<number>(defaultIndex);

  // Handle navigation in different directions
  const handleNavigation = useCallback((key: NavigationKey): boolean => {
    if (!enabled || itemCount === 0) return false;

    // Handle Home and End keys
    if (key === 'Home') {
      setFocusedIndex(0);
      return true;
    }
    
    if (key === 'End') {
      setFocusedIndex(itemCount - 1);
      return true;
    }

    // Get current index, defaulting to first item if none focused
    const currentIndex = focusedIndex >= 0 ? focusedIndex : 0;
    
    // Determine if we should handle this key based on direction
    const shouldHandleVertical = 
      (direction === 'vertical' || direction === 'both') && 
      (key === 'ArrowUp' || key === 'ArrowDown');
      
    const shouldHandleHorizontal = 
      (direction === 'horizontal' || direction === 'both') && 
      (key === 'ArrowLeft' || key === 'ArrowRight');
      
    if (!shouldHandleVertical && !shouldHandleHorizontal) {
      return false;
    }

    // Calculate new index
    let newIndex = currentIndex;
    
    switch (key) {
      case 'ArrowUp':
      case 'ArrowLeft':
        newIndex = currentIndex - 1;
        if (newIndex < 0) {
          newIndex = wrap ? itemCount - 1 : 0;
        }
        break;
      case 'ArrowDown':
      case 'ArrowRight':
        newIndex = currentIndex + 1;
        if (newIndex >= itemCount) {
          newIndex = wrap ? 0 : itemCount - 1;
        }
        break;
    }

    // Only update if the index actually changed
    if (newIndex !== currentIndex) {
      setFocusedIndex(newIndex);
      return true;
    }
    
    return false;
  }, [enabled, itemCount, focusedIndex, direction, wrap]);
  
  // Handle key down event
  const handleKeyDown = useCallback((e: ReactKeyboardEvent | KeyboardEvent) => {
    const key = e.key;
    
    if (key === 'Enter' || key === ' ') {
      if (focusedIndex >= 0 && onSelect) {
        e.preventDefault();
        onSelect(focusedIndex);
        return;
      }
    }
    
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(key)) {
      const handled = handleNavigation(key as NavigationKey);
      if (handled) {
        e.preventDefault();
      }
    }
  }, [handleNavigation, focusedIndex, onSelect]);

  // Focus the DOM element when focusedIndex changes
  useEffect(() => {
    if (!containerRef?.current || focusedIndex < 0) return;
    
    // Find all items within the container
    const items = Array.from(
      containerRef.current.querySelectorAll<HTMLElement>(itemSelector)
    );
    
    // If we have an item at the focused index, focus it
    if (items[focusedIndex]) {
      items[focusedIndex].focus();
    }
  }, [focusedIndex, containerRef, itemSelector]);

  return {
    focusedIndex,
    setFocusedIndex,
    handleKeyDown,
    handleNavigation
  };
} 