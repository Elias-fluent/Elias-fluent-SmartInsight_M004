import { useEffect, useRef } from 'react';
import type { RefObject } from 'react';

interface UseFocusTrapOptions {
  // If true, focus will be trapped only when the component is active/visible
  active?: boolean;
  // If true, will auto-focus the first focusable element when activated
  autoFocus?: boolean;
  // If provided, will focus this element when trap is activated
  initialFocusRef?: RefObject<HTMLElement>;
  // If true, will restore focus to previously focused element when deactivated
  restoreFocus?: boolean;
  // Called when escape key is pressed while trap is active
  onEscape?: () => void;
}

/**
 * Hook that traps focus within a container element
 * 
 * @param containerRef - Ref to the container element to trap focus within
 * @param options - Configuration options
 * @returns Object with ref to attach to the container
 * 
 * @example
 * ```tsx
 * const Modal = ({ isOpen, onClose }) => {
 *   const modalRef = useRef(null);
 *   useFocusTrap(modalRef, { 
 *     active: isOpen, 
 *     onEscape: onClose,
 *     restoreFocus: true
 *   });
 *   
 *   return isOpen ? (
 *     <div ref={modalRef} role="dialog" aria-modal="true">
 *       <button>Focusable button</button>
 *       <button>Another button</button>
 *       <button onClick={onClose}>Close</button>
 *     </div>
 *   ) : null;
 * }
 * ```
 */
export function useFocusTrap(
  containerRef: RefObject<HTMLElement>,
  options: UseFocusTrapOptions = {}
) {
  const {
    active = true,
    autoFocus = true,
    initialFocusRef,
    restoreFocus = true,
    onEscape,
  } = options;
  
  // Store the element that had focus before the trap was activated
  const previouslyFocusedElement = useRef<HTMLElement | null>(null);

  useEffect(() => {
    // Only apply the focus trap if it's active
    if (!active) return;

    const container = containerRef.current;
    if (!container) return;

    // Store the currently focused element to restore later
    if (restoreFocus) {
      previouslyFocusedElement.current = document.activeElement as HTMLElement;
    }

    // Handle initial focus
    if (autoFocus) {
      if (initialFocusRef && initialFocusRef.current) {
        initialFocusRef.current.focus();
      } else {
        // Focus the first focusable element in the container
        const focusableElements = getFocusableElements(container);
        if (focusableElements.length > 0) {
          focusableElements[0].focus();
        } else {
          // If no focusable elements, focus the container itself
          container.setAttribute('tabindex', '-1');
          container.focus();
        }
      }
    }

    // Handle tab key to cycle through focusable elements
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && onEscape) {
        event.preventDefault();
        onEscape();
        return;
      }

      if (event.key !== 'Tab') return;

      const focusableElements = getFocusableElements(container);
      if (focusableElements.length === 0) return;

      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];
      const { activeElement } = document;

      // Shift+Tab on first element should wrap to last element
      if (event.shiftKey && activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      } 
      // Tab on last element should wrap to first element
      else if (!event.shiftKey && activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    };

    // Prevent focus from leaving the trap
    const handleFocusIn = (event: FocusEvent) => {
      if (!container.contains(event.target as Node)) {
        // Focus moved outside the container, pull it back in
        const focusableElements = getFocusableElements(container);
        if (focusableElements.length > 0) {
          event.preventDefault();
          focusableElements[0].focus();
        }
      }
    };

    // Add event listeners
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('focusin', handleFocusIn);

    // Cleanup function
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('focusin', handleFocusIn);

      // Restore focus to previously focused element when trap is deactivated
      if (restoreFocus && previouslyFocusedElement.current) {
        previouslyFocusedElement.current.focus();
      }
    };
  }, [active, autoFocus, initialFocusRef, onEscape, restoreFocus, containerRef]);

  return { containerRef };
}

/**
 * Gets all focusable elements within a container
 */
function getFocusableElements(container: HTMLElement): HTMLElement[] {
  // CSS selectors for all potentially focusable elements
  const selector = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
    'details',
    'summary',
  ].join(',');

  // Convert NodeList to Array and filter out any hidden elements
  return Array.from(container.querySelectorAll<HTMLElement>(selector))
    .filter(el => {
      // Filter out elements that are not visible/hidden
      return !!(
        el.offsetWidth ||
        el.offsetHeight ||
        el.getClientRects().length
      );
    });
} 