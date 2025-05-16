import { useRef, useCallback, useEffect } from 'react';

type AnnouncementPolitenesss = 'polite' | 'assertive';

interface UseAnnounceOptions {
  // How urgent the announcement is (polite = wait for current speech to finish, assertive = interrupt)
  politeness?: AnnouncementPolitenesss;
  // How long (ms) to wait before removing message from live region (helps with some screen readers)
  clearDelay?: number;
  // Text to use in aria role attributes for the announcer
  regionLabel?: string;
}

/**
 * Hook for accessible screen reader announcements using ARIA live regions
 * 
 * @param options - Configuration options
 * @returns Object with announcement functions
 * 
 * @example
 * ```tsx
 * const MyComponent = () => {
 *   const { announce } = useAnnounce();
 *   
 *   const handleAction = () => {
 *     // Do something
 *     announce('Action completed successfully');
 *   };
 *   
 *   return (
 *     <button onClick={handleAction}>
 *       Perform Action
 *     </button>
 *   );
 * }
 * ```
 */
export function useAnnounce(options: UseAnnounceOptions = {}) {
  const {
    politeness = 'polite',
    clearDelay = 5000,
    regionLabel = 'Status announcement'
  } = options;
  
  // Refs to track announcer elements
  const politeAnnouncerRef = useRef<HTMLDivElement | null>(null);
  const assertiveAnnouncerRef = useRef<HTMLDivElement | null>(null);
  
  // Setup the live regions on mount
  useEffect(() => {
    // Create polite announcer if it doesn't exist
    if (!politeAnnouncerRef.current) {
      politeAnnouncerRef.current = document.createElement('div');
      const politeAnnouncer = politeAnnouncerRef.current;
      
      // Set attributes for accessibility
      politeAnnouncer.setAttribute('aria-live', 'polite');
      politeAnnouncer.setAttribute('aria-atomic', 'true');
      politeAnnouncer.setAttribute('aria-relevant', 'additions text');
      politeAnnouncer.setAttribute('aria-label', regionLabel);
      
      // Hide visually but keep accessible to screen readers
      Object.assign(politeAnnouncer.style, {
        position: 'absolute',
        width: '1px',
        height: '1px',
        padding: '0',
        overflow: 'hidden',
        clip: 'rect(0, 0, 0, 0)',
        whiteSpace: 'nowrap',
        border: '0',
      });
      
      // Add to DOM
      document.body.appendChild(politeAnnouncer);
    }
    
    // Create assertive announcer if it doesn't exist
    if (!assertiveAnnouncerRef.current) {
      assertiveAnnouncerRef.current = document.createElement('div');
      const assertiveAnnouncer = assertiveAnnouncerRef.current;
      
      // Set attributes for accessibility
      assertiveAnnouncer.setAttribute('aria-live', 'assertive');
      assertiveAnnouncer.setAttribute('aria-atomic', 'true');
      assertiveAnnouncer.setAttribute('aria-relevant', 'additions text');
      assertiveAnnouncer.setAttribute('aria-label', regionLabel);
      
      // Hide visually but keep accessible to screen readers
      Object.assign(assertiveAnnouncer.style, {
        position: 'absolute',
        width: '1px',
        height: '1px',
        padding: '0',
        overflow: 'hidden',
        clip: 'rect(0, 0, 0, 0)',
        whiteSpace: 'nowrap',
        border: '0',
      });
      
      // Add to DOM
      document.body.appendChild(assertiveAnnouncer);
    }
    
    // Clean up on unmount
    return () => {
      if (politeAnnouncerRef.current) {
        document.body.removeChild(politeAnnouncerRef.current);
        politeAnnouncerRef.current = null;
      }
      
      if (assertiveAnnouncerRef.current) {
        document.body.removeChild(assertiveAnnouncerRef.current);
        assertiveAnnouncerRef.current = null;
      }
    };
  }, [regionLabel]);
  
  // Function to announce a message
  const announce = useCallback((message: string, announcePoliteness?: AnnouncementPolitenesss) => {
    const actualPoliteness = announcePoliteness || politeness;
    const announcer = actualPoliteness === 'assertive' 
      ? assertiveAnnouncerRef.current 
      : politeAnnouncerRef.current;
      
    if (!announcer) return;
    
    // Clear any existing text first (helps ensure announcement in some screen readers)
    announcer.textContent = '';
    
    // Use setTimeout to ensure clearing happens in separate tick
    setTimeout(() => {
      if (announcer) {
        announcer.textContent = message;
        
        // Clear after delay for better support across screen readers
        if (clearDelay > 0) {
          setTimeout(() => {
            if (announcer) {
              announcer.textContent = '';
            }
          }, clearDelay);
        }
      }
    }, 50);
  }, [politeness, clearDelay]);
  
  // Convenience method for assertive announcements
  const announceAssertive = useCallback((message: string) => {
    announce(message, 'assertive');
  }, [announce]);
  
  // Convenience method for polite announcements
  const announcePolite = useCallback((message: string) => {
    announce(message, 'polite');
  }, [announce]);
  
  return {
    announce,
    announceAssertive,
    announcePolite
  };
} 