import React, { useEffect, useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { MoreHorizontal, Copy, Download, Trash } from 'lucide-react';
import type { Message } from '../../store/slices/chatSlice';
import { useChat } from '../../hooks/useChat';
import { CHAT_ACTIONS } from '../../store/slices/chatSlice';
import { useFocusVisible } from '../../hooks/useFocusVisible';
import { useAnnounce } from '../../hooks/useAnnounce';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';

interface MessageActionsProps {
  message: Message;
  conversationId: string;
}

const MessageActions: React.FC<MessageActionsProps> = ({ message, conversationId }) => {
  const [isOpen, setIsOpen] = useState(false);
  const { dispatch } = useChat();
  const menuRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const { isFocusVisible } = useFocusVisible();
  const { announce } = useAnnounce();
  
  // Setup keyboard navigation for menu items
  const { focusedIndex, handleKeyDown } = useKeyboardNavigation({
    itemCount: 3, // Copy, Save, Delete
    direction: 'vertical',
    wrap: true,
    containerRef: menuRef as React.RefObject<HTMLElement>,
    itemSelector: '[role="menuitem"]',
    enabled: isOpen,
  });
  
  const toggleMenu = () => {
    const newState = !isOpen;
    setIsOpen(newState);
    if (newState) {
      // Announce menu opening for screen readers
      announce('Message actions menu opened');
    }
  };
  
  // Handle keyboard events on the toggle button
  const handleButtonKeyDown = (e: KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
      e.preventDefault();
      if (!isOpen) {
        toggleMenu();
      }
    } else if (e.key === 'Escape' && isOpen) {
      e.preventDefault();
      setIsOpen(false);
      announce('Menu closed');
    }
  };
  
  // Handle keyboard events on the menu
  const handleMenuKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    // First let the keyboard navigation hook handle arrow keys
    handleKeyDown(e);
    
    // Handle Escape to close the menu
    if (e.key === 'Escape') {
      e.preventDefault();
      setIsOpen(false);
      buttonRef.current?.focus();
      announce('Menu closed');
    }
  };
  
  // Close when clicking outside
  useEffect(() => {
    if (!isOpen) return;
    
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node) && 
          buttonRef.current && !buttonRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);
  
  // Handle the copy action
  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigator.clipboard.writeText(message.content);
    setIsOpen(false);
    announce('Message copied to clipboard');
  };
  
  // Handle the save action (downloads as text file)
  const handleSave = (e: React.MouseEvent) => {
    e.stopPropagation();
    
    // Create file content
    const sender = message.role === 'user' ? 'You' : 'Assistant';
    const timestamp = new Date(message.timestamp).toLocaleString();
    const fileContent = `Sender: ${sender}\nTimestamp: ${timestamp}\n\n${message.content}`;
    
    // Create download link
    const element = document.createElement('a');
    const file = new Blob([fileContent], { type: 'text/plain' });
    element.href = URL.createObjectURL(file);
    element.download = `message-${message.id}.txt`;
    
    // Trigger download
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
    
    setIsOpen(false);
    announce('Message saved as file');
  };
  
  // Handle the delete action
  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (window.confirm('Are you sure you want to delete this message?')) {
      dispatch({
        type: CHAT_ACTIONS.DELETE_MESSAGE,
        payload: { conversationId, messageId: message.id }
      });
      announce('Message deleted');
    }
    
    setIsOpen(false);
  };
  
  return (
    <div className="relative">
      <button 
        ref={buttonRef}
        onClick={toggleMenu}
        onKeyDown={handleButtonKeyDown}
        className={`p-1 rounded-full hover:bg-muted focus:outline-none ${isFocusVisible ? 'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2' : ''}`}
        aria-label="Message actions"
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-controls={isOpen ? "message-actions-menu" : undefined}
      >
        <MoreHorizontal size={16} aria-hidden="true" />
      </button>
      
      {isOpen && (
        <div 
          ref={menuRef}
          className="absolute right-0 mt-1 w-48 rounded-md shadow-lg bg-background border border-border z-20"
          id="message-actions-menu"
          role="menu"
          aria-orientation="vertical"
          aria-labelledby="message-actions-button"
          onKeyDown={handleMenuKeyDown}
        >
          <div className="py-1">
            <button
              className={`flex items-center w-full px-4 py-2 text-sm text-left hover:bg-muted ${focusedIndex === 0 && isFocusVisible ? 'ring-2 ring-inset ring-ring' : ''}`}
              onClick={handleCopy}
              role="menuitem"
              tabIndex={focusedIndex === 0 ? 0 : -1}
              aria-label="Copy to clipboard"
            >
              <Copy size={16} className="mr-2" aria-hidden="true" />
              Copy to clipboard
            </button>
            <button
              className={`flex items-center w-full px-4 py-2 text-sm text-left hover:bg-muted ${focusedIndex === 1 && isFocusVisible ? 'ring-2 ring-inset ring-ring' : ''}`}
              onClick={handleSave}
              role="menuitem"
              tabIndex={focusedIndex === 1 ? 0 : -1}
              aria-label="Save as file"
            >
              <Download size={16} className="mr-2" aria-hidden="true" />
              Save as file
            </button>
            <button
              className={`flex items-center w-full px-4 py-2 text-sm text-left text-destructive hover:bg-muted ${focusedIndex === 2 && isFocusVisible ? 'ring-2 ring-inset ring-ring' : ''}`}
              onClick={handleDelete}
              role="menuitem"
              tabIndex={focusedIndex === 2 ? 0 : -1}
              aria-label="Delete message"
            >
              <Trash size={16} className="mr-2" aria-hidden="true" />
              Delete message
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default MessageActions; 