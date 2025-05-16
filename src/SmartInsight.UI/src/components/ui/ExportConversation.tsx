import React, { useRef, useState, useEffect } from 'react';
import type { KeyboardEvent } from 'react';
import type { Conversation } from '../../store/slices/chatSlice';
import { Download, File, FileJson, FileText, X } from 'lucide-react';
import { useFocusVisible } from '../../hooks/useFocusVisible';
import { useAnnounce } from '../../hooks/useAnnounce';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';

interface ExportConversationProps {
  conversation: Conversation;
  isOpen: boolean;
  onClose: () => void;
}

const ExportConversation: React.FC<ExportConversationProps> = ({
  conversation,
  isOpen,
  onClose,
}) => {
  const [selectedFormat, setSelectedFormat] = useState<'pdf' | 'txt' | 'json'>('pdf');
  const modalRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const formatsContainerRef = useRef<HTMLDivElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const { isFocusVisible } = useFocusVisible();
  const { announce } = useAnnounce();
  
  // Implement focus trap and restore focus when modal closes
  useEffect(() => {
    if (!isOpen) return;
    
    // Store previously focused element to restore later
    previousFocusRef.current = document.activeElement as HTMLElement;
    
    // Focus the close button when modal opens
    if (closeButtonRef.current) {
      closeButtonRef.current.focus();
    }
    
    // Handle tab key to keep focus within modal
    const handleTabKey = (e: KeyboardEvent) => {
      if (e.key !== 'Tab' || !modalRef.current) return;
      
      // Get all focusable elements within the modal
      const focusableElements = Array.from(
        modalRef.current.querySelectorAll(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        )
      ) as HTMLElement[];
      
      if (focusableElements.length === 0) return;
      
      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];
      
      // Handle Tab and Shift+Tab to cycle within the modal
      if (e.shiftKey && document.activeElement === firstElement) {
        e.preventDefault();
        lastElement.focus();
      } else if (!e.shiftKey && document.activeElement === lastElement) {
        e.preventDefault();
        firstElement.focus();
      }
    };
    
    // Handle escape key to close modal
    const handleEscapeKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.preventDefault();
        onClose();
      }
    };
    
    // Combine key handlers
    const handleKeyDown = (e: any) => {
      handleTabKey(e);
      handleEscapeKey(e);
    };
    
    // Add event listener for keyboard navigation
    document.addEventListener('keydown', handleKeyDown);
    
    // Cleanup function
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      
      // Restore focus when modal closes
      if (previousFocusRef.current && !isOpen) {
        previousFocusRef.current.focus();
      }
    };
  }, [isOpen, onClose]);
  
  // Setup keyboard navigation for format options
  const { focusedIndex, handleKeyDown } = useKeyboardNavigation({
    itemCount: 3, // PDF, TXT, JSON
    direction: 'horizontal',
    wrap: true,
    containerRef: formatsContainerRef as React.RefObject<HTMLElement>,
    itemSelector: '[role="radio"]',
    enabled: isOpen,
    onSelect: (index) => {
      const formats: Array<'pdf' | 'txt' | 'json'> = ['pdf', 'txt', 'json'];
      if (index >= 0 && index < formats.length) {
        setSelectedFormat(formats[index]);
        announce(`Selected ${formats[index].toUpperCase()} format`);
      }
    }
  });
  
  // Handle keyboard events on the formats container
  const handleFormatsKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    handleKeyDown(e);
  };

  // Handle the export action
  const handleExport = () => {
    // For demonstration - in a real app, implement actual export for each format
    switch (selectedFormat) {
      case 'pdf':
        // Example - Replace with actual PDF generation and download
        announce('Exporting conversation as PDF');
        console.log('Exporting conversation as PDF', conversation);
        break;
      case 'txt':
        // Generate conversation text
        const textContent = generateTextContent(conversation);
        downloadFile(textContent, `conversation-${conversation.id}.txt`, 'text/plain');
        announce('Conversation exported as text file');
        break;
      case 'json':
        // Export JSON representation
        const jsonContent = JSON.stringify(conversation, null, 2);
        downloadFile(jsonContent, `conversation-${conversation.id}.json`, 'application/json');
        announce('Conversation exported as JSON file');
        break;
    }
    
    // Close the modal after export
    onClose();
  };
  
  // Generate plain text content for TXT export
  const generateTextContent = (conversation: Conversation): string => {
    let content = `Conversation: ${conversation.title || 'Untitled'}\n`;
    content += `Date: ${new Date(conversation.createdAt).toLocaleString()}\n\n`;
    
    conversation.messages.forEach(message => {
      const sender = message.role === 'user' ? 'You' : 'Assistant';
      const time = new Date(message.timestamp).toLocaleTimeString();
      content += `[${time}] ${sender}:\n${message.content}\n\n`;
    });
    
    return content;
  };
  
  // Helper to download file
  const downloadFile = (content: string, filename: string, contentType: string) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };
  
  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      role="dialog"
      aria-modal="true"
      aria-labelledby="export-dialog-title"
    >
      <div 
        ref={modalRef}
        className="bg-background rounded-lg shadow-lg w-full max-w-md p-6 relative animate-in fade-in"
      >
        <button
          ref={closeButtonRef}
          className={`absolute right-4 top-4 p-1 rounded-full hover:bg-muted focus:outline-none ${
            isFocusVisible ? 'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2' : ''
          }`}
          onClick={onClose}
          aria-label="Close export dialog"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
        
        <h2 id="export-dialog-title" className="text-lg font-bold mb-4">
          Export Conversation
        </h2>
        
        <p className="text-sm text-muted-foreground mb-6">
          Choose a format to export your conversation
        </p>
        
        <div 
          ref={formatsContainerRef}
          className="flex justify-center space-x-4 mb-6"
          role="radiogroup"
          aria-label="Export format"
          onKeyDown={handleFormatsKeyDown}
        >
          <button
            role="radio"
            aria-checked={selectedFormat === 'pdf'}
            tabIndex={focusedIndex === 0 ? 0 : -1}
            className={`flex flex-col items-center p-4 border rounded ${
              selectedFormat === 'pdf' 
                ? 'border-primary bg-primary/10' 
                : 'border-border hover:border-primary/50'
            } ${isFocusVisible && focusedIndex === 0 ? 'ring-2 ring-ring ring-offset-2' : ''}`}
            onClick={() => setSelectedFormat('pdf')}
          >
            <File className="h-8 w-8 mb-2" aria-hidden="true" />
            <span>PDF</span>
          </button>
          
          <button
            role="radio"
            aria-checked={selectedFormat === 'txt'}
            tabIndex={focusedIndex === 1 ? 0 : -1}
            className={`flex flex-col items-center p-4 border rounded ${
              selectedFormat === 'txt' 
                ? 'border-primary bg-primary/10' 
                : 'border-border hover:border-primary/50'
            } ${isFocusVisible && focusedIndex === 1 ? 'ring-2 ring-ring ring-offset-2' : ''}`}
            onClick={() => setSelectedFormat('txt')}
          >
            <FileText className="h-8 w-8 mb-2" aria-hidden="true" />
            <span>TXT</span>
          </button>
          
          <button
            role="radio"
            aria-checked={selectedFormat === 'json'}
            tabIndex={focusedIndex === 2 ? 0 : -1}
            className={`flex flex-col items-center p-4 border rounded ${
              selectedFormat === 'json' 
                ? 'border-primary bg-primary/10' 
                : 'border-border hover:border-primary/50'
            } ${isFocusVisible && focusedIndex === 2 ? 'ring-2 ring-ring ring-offset-2' : ''}`}
            onClick={() => setSelectedFormat('json')}
          >
            <FileJson className="h-8 w-8 mb-2" aria-hidden="true" />
            <span>JSON</span>
          </button>
        </div>
        
        <div className="flex justify-end">
          <button
            className={`px-4 py-2 bg-muted text-foreground rounded mr-2 hover:bg-muted/80 focus:outline-none ${
              isFocusVisible ? 'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2' : ''
            }`}
            onClick={onClose}
          >
            Cancel
          </button>
          
          <button
            className={`px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90 focus:outline-none ${
              isFocusVisible ? 'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2' : ''
            }`}
            onClick={handleExport}
          >
            <Download className="h-4 w-4 mr-2 inline-block" aria-hidden="true" />
            Export
          </button>
        </div>
      </div>
    </div>
  );
};

export default ExportConversation; 