import React, { useEffect, useRef } from 'react';
import { useUI } from '../../store/StoreContext';
import { uiActions } from '../../store/slices/uiSlice';
import AccessibilitySettings from './AccessibilitySettings';
import { useAnnounce } from '../../hooks/useAnnounce';

interface AccessibilityModalProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * Modal component for accessibility settings
 * Renders AccessibilitySettings in a modal dialog with proper focus management
 */
export function AccessibilityModal({ isOpen, onClose }: AccessibilityModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const { announce } = useAnnounce();
  const previousFocusRef = useRef<HTMLElement | null>(null);
  
  // Handle focus trap and keyboard navigation
  useEffect(() => {
    if (!isOpen) return;
    
    const modalElement = modalRef.current;
    if (!modalElement) return;
    
    // Store currently focused element
    previousFocusRef.current = document.activeElement as HTMLElement;
    
    // Get all focusable elements in the modal
    const focusableElements = modalElement.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    
    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];
    
    // Focus first element
    if (focusableElements.length > 0) {
      setTimeout(() => {
        firstElement.focus();
      }, 50);
    }
    
    const handleKeyDown = (e: KeyboardEvent) => {
      // Close on escape
      if (e.key === 'Escape') {
        e.preventDefault();
        onClose();
        return;
      }
      
      // Handle tab key navigation
      if (e.key === 'Tab') {
        // If shift+tab on first element, move to last element
        if (e.shiftKey && document.activeElement === firstElement) {
          e.preventDefault();
          lastElement.focus();
        }
        // If tab on last element, cycle to first element
        else if (!e.shiftKey && document.activeElement === lastElement) {
          e.preventDefault();
          firstElement.focus();
        }
      }
    };
    
    // Keep focus inside the modal
    const handleFocusIn = (e: FocusEvent) => {
      if (modalElement && !modalElement.contains(e.target as Node)) {
        firstElement.focus();
      }
    };
    
    // Add event listeners
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('focusin', handleFocusIn);
    
    return () => {
      // Clean up event listeners
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('focusin', handleFocusIn);
      
      // Restore focus to previous element
      if (previousFocusRef.current) {
        setTimeout(() => {
          previousFocusRef.current?.focus();
        }, 0);
      }
    };
  }, [isOpen, onClose]);
  
  // Manage body scroll lock
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden'; // Prevent scrolling
      announce('Accessibility settings dialog opened');
    } else {
      document.body.style.overflow = ''; // Restore scrolling
    }
    
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen, announce]);
  
  if (!isOpen) return null;
  
  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      aria-modal="true"
      role="dialog"
      aria-labelledby="accessibility-settings-heading"
    >
      {/* Click outside to close */}
      <div 
        className="absolute inset-0" 
        onClick={onClose}
        aria-hidden="true"
      />
      
      {/* Modal content */}
      <div 
        ref={modalRef}
        className="relative bg-background rounded-lg shadow-lg max-w-md w-full max-h-[90vh] overflow-y-auto"
        onClick={e => e.stopPropagation()}
        tabIndex={-1} // Make sure the container is focusable as a fallback
      >
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute top-3 right-3 rounded-full p-2 hover:bg-muted focus:outline-none focus:ring-2 focus:ring-primary"
          aria-label="Close accessibility settings"
        >
          <svg 
            xmlns="http://www.w3.org/2000/svg" 
            width="24" 
            height="24" 
            viewBox="0 0 24 24" 
            fill="none" 
            stroke="currentColor" 
            strokeWidth="2" 
            strokeLinecap="round" 
            strokeLinejoin="round"
          >
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
        
        {/* Settings content */}
        <AccessibilitySettings className="p-6" />
      </div>
    </div>
  );
}

/**
 * Button to open the accessibility settings modal
 */
export function AccessibilityButton() {
  const { ui, dispatch } = useUI();
  
  const openModal = () => {
    dispatch(uiActions.setModal('accessibility'));
  };
  
  const closeModal = () => {
    dispatch(uiActions.setModal(null));
  };
  
  const isOpen = ui.activeModal === 'accessibility';
  
  return (
    <>
      <button
        onClick={openModal}
        className="flex items-center gap-2 px-3 py-2 rounded-md hover:bg-secondary focus:outline-none focus:ring-2 focus:ring-primary"
        aria-label="Open accessibility settings"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="12" cy="12" r="10"></circle>
          <circle cx="12" cy="12" r="4"></circle>
          <line x1="4.93" y1="4.93" x2="9.17" y2="9.17"></line>
          <line x1="14.83" y1="14.83" x2="19.07" y2="19.07"></line>
          <line x1="14.83" y1="9.17" x2="19.07" y2="4.93"></line>
          <line x1="14.83" y1="9.17" x2="18.36" y2="5.64"></line>
          <line x1="4.93" y1="19.07" x2="9.17" y2="14.83"></line>
        </svg>
        <span>Accessibility</span>
      </button>
      
      <AccessibilityModal isOpen={isOpen} onClose={closeModal} />
    </>
  );
}

export default AccessibilityButton; 