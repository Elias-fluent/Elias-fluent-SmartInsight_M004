import React, { useState } from 'react';
import { MoreHorizontal, Copy, Download, Share, Trash } from 'lucide-react';
import type { Message } from '../../store/slices/chatSlice';
import { useChat } from '../../store/StoreContext';
import { chatActions } from '../../store/slices/chatSlice';

interface MessageActionsProps {
  message: Message;
  conversationId: string;
}

const MessageActions: React.FC<MessageActionsProps> = ({ message, conversationId }) => {
  const [isOpen, setIsOpen] = useState(false);
  const { dispatch } = useChat();
  
  const toggleMenu = () => {
    setIsOpen(!isOpen);
  };
  
  // Close when clicking outside
  const handleClickOutside = () => {
    if (isOpen) setIsOpen(false);
  };
  
  // Handle the copy action
  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigator.clipboard.writeText(message.content);
    setIsOpen(false);
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
  };
  
  // Handle the delete action
  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (window.confirm('Are you sure you want to delete this message?')) {
      dispatch(chatActions.deleteMessage(conversationId, message.id));
    }
    
    setIsOpen(false);
  };
  
  return (
    <div className="relative">
      <button 
        onClick={toggleMenu}
        className="p-1 rounded-full hover:bg-muted"
        aria-label="Message actions"
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <MoreHorizontal size={16} />
      </button>
      
      {isOpen && (
        <>
          <div 
            className="fixed inset-0 z-10" 
            onClick={handleClickOutside}
          />
          <div className="absolute right-0 mt-1 w-48 rounded-md shadow-lg bg-background border border-border z-20">
            <div className="py-1" role="menu" aria-orientation="vertical">
              <button
                className="flex items-center w-full px-4 py-2 text-sm text-left hover:bg-muted"
                onClick={handleCopy}
                role="menuitem"
              >
                <Copy size={16} className="mr-2" />
                Copy to clipboard
              </button>
              <button
                className="flex items-center w-full px-4 py-2 text-sm text-left hover:bg-muted"
                onClick={handleSave}
                role="menuitem"
              >
                <Download size={16} className="mr-2" />
                Save as file
              </button>
              <button
                className="flex items-center w-full px-4 py-2 text-sm text-left text-destructive hover:bg-muted"
                onClick={handleDelete}
                role="menuitem"
              >
                <Trash size={16} className="mr-2" />
                Delete message
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default MessageActions; 