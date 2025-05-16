import React, { useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import type { Conversation, Message } from '../../store/slices/chatSlice';
import { Clock, Copy, Download, Share2, Trash2 } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import remarkGfm from 'remark-gfm';
import { useAnnounce } from '../../hooks/useAnnounce';
import { useFocusVisible } from '../../hooks/useFocusVisible';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import ExportConversation from './ExportConversation';

interface ConversationDetailProps {
  conversation: Conversation | null;
  onExport?: (conversation: Conversation) => void;
  onDelete?: (conversationId: string) => void;
  onShare?: (conversation: Conversation) => void;
  className?: string;
}

const ConversationDetail: React.FC<ConversationDetailProps> = ({
  conversation,
  onExport,
  onDelete,
  onShare,
  className = '',
}) => {
  const { announce } = useAnnounce();
  const containerRef = useRef<HTMLDivElement>(null);
  const messagesRef = useRef<HTMLDivElement>(null);
  const [showExportModal, setShowExportModal] = useState(false);
  
  // Setup keyboard navigation for the message list
  const { focusedIndex, handleKeyDown } = useKeyboardNavigation({
    itemCount: conversation?.messages.length || 0,
    direction: 'vertical',
    wrap: true,
    containerRef: messagesRef as React.RefObject<HTMLElement>,
    itemSelector: '[role="complementary"], [role="article"]'
  });

  // Function to copy message content to clipboard
  const copyMessageToClipboard = (content: string) => {
    navigator.clipboard.writeText(content);
    // Announce for screen readers
    announce('Message copied to clipboard');
    // In a real app, show a notification
    console.log('Copied to clipboard');
  };

  // Format timestamp to readable format
  const formatTimestamp = (timestamp: number): string => {
    const date = new Date(timestamp);
    return date.toLocaleString();
  };
  
  // Handle keyboard events at the container level
  const handleContainerKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    // Pass to the keyboard navigation handler
    handleKeyDown(e);
    
    // Handle the Escape key to provide an escape hatch
    if (e.key === 'Escape') {
      // Move focus back to the conversation header or another appropriate place
      const header = containerRef.current?.querySelector('h2');
      if (header) {
        (header as HTMLElement).focus();
      }
    }
  };
  
  // Handle export button click
  const handleExportClick = () => {
    setShowExportModal(true);
    announce('Export conversation dialog opened');
  };
  
  // Handle export modal close
  const handleExportModalClose = () => {
    setShowExportModal(false);
    announce('Export conversation dialog closed');
  };

  if (!conversation) {
    return (
      <div 
        className={`flex flex-col items-center justify-center h-full ${className}`}
        role="region"
        aria-label="Conversation details"
      >
        <p className="text-lg text-muted-foreground mb-2">No conversation selected</p>
        <p className="text-sm text-muted-foreground">Select a conversation from the list to view its details</p>
      </div>
    );
  }

  return (
    <div 
      className={`flex flex-col h-full ${className}`}
      ref={containerRef}
      role="region"
      aria-label={`Conversation: ${conversation.title || 'Untitled conversation'}`}
    >
      {/* Conversation header */}
      <div className="flex justify-between items-center pb-3 border-b mb-4">
        <div>
          <h2 className="text-xl font-bold" id="conversation-title" tabIndex={-1}>
            {conversation.title || 'Untitled conversation'}
          </h2>
          <div className="flex items-center text-xs text-muted-foreground mt-1" aria-live="polite">
            <Clock className="h-3 w-3 mr-1" aria-hidden="true" />
            <span>{formatTimestamp(conversation.createdAt)}</span>
          </div>
        </div>
        
        {/* Action buttons */}
        <div className="flex space-x-2" role="toolbar" aria-label="Conversation actions">
          {onShare && (
            <button
              onClick={() => {
                onShare(conversation);
                announce('Sharing conversation');
              }}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              aria-label="Share conversation"
            >
              <Share2 className="h-4 w-4" aria-hidden="true" />
            </button>
          )}
          
          {onExport && (
            <button
              onClick={handleExportClick}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              aria-label="Export conversation"
            >
              <Download className="h-4 w-4" aria-hidden="true" />
            </button>
          )}
          
          {onDelete && (
            <button
              onClick={() => {
                onDelete(conversation.id);
                announce('Conversation deleted');
              }}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              aria-label="Delete conversation"
            >
              <Trash2 className="h-4 w-4" aria-hidden="true" />
            </button>
          )}
        </div>
      </div>
      
      {/* Conversation messages */}
      <div 
        className="flex-1 overflow-y-auto space-y-6 pr-1"
        ref={messagesRef}
        role="log"
        aria-label="Conversation messages"
        aria-describedby="conversation-title"
        onKeyDown={handleContainerKeyDown}
      >
        {conversation.messages.map((message, index) => (
          <MessageItem
            key={message.id}
            message={message}
            onCopy={() => copyMessageToClipboard(message.content)}
            index={index}
            total={conversation.messages.length}
            isFocused={focusedIndex === index}
          />
        ))}
      </div>
      
      {/* Export Modal */}
      {showExportModal && conversation && (
        <ExportConversation
          conversation={conversation}
          isOpen={showExportModal}
          onClose={handleExportModalClose}
        />
      )}
    </div>
  );
};

// Message item component
interface MessageItemProps {
  message: Message;
  onCopy: () => void;
  index: number;
  total: number;
  isFocused?: boolean;
}

const MessageItem: React.FC<MessageItemProps> = ({ message, onCopy, index, total, isFocused = false }) => {
  const isUser = message.role === 'user';
  const { isFocusVisible } = useFocusVisible();
  const messageRef = useRef<HTMLDivElement>(null);
  
  // Handle keyboard events within this message item
  const handleKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    // Handle Enter or Space to copy the message content
    if ((e.key === 'Enter' || e.key === ' ') && e.target === messageRef.current) {
      e.preventDefault();
      onCopy();
    }
  };
  
  return (
    <div 
      ref={messageRef}
      className={`relative group ${isUser ? 'ml-10' : 'mr-10'}`}
      role={isUser ? "complementary" : "article"}
      aria-label={`${isUser ? 'Your message' : 'Assistant response'} ${index + 1} of ${total}`}
      tabIndex={isFocused ? 0 : -1}
      onKeyDown={handleKeyDown}
    >
      <div
        className={`p-4 rounded-lg ${
          isUser
            ? 'bg-primary text-primary-foreground'
            : 'bg-muted text-foreground'
        } ${isFocusVisible && isFocused ? 'ring-2 ring-ring ring-offset-2' : ''}`}
      >
        <div className="flex justify-between items-start mb-2">
          <div className="font-medium">
            {isUser ? 'You' : 'Assistant'}
          </div>
          <div className="text-xs opacity-70">
            <time dateTime={new Date(message.timestamp).toISOString()}>
              {new Date(message.timestamp).toLocaleTimeString()}
            </time>
          </div>
        </div>
        
        {/* Message content with markdown support */}
        <div className="prose prose-sm dark:prose-invert max-w-none">
          <ReactMarkdown
            rehypePlugins={[rehypeSanitize]}
            remarkPlugins={[remarkGfm]}
          >
            {message.content}
          </ReactMarkdown>
        </div>
        
        {/* Message status */}
        {message.status === 'error' && (
          <div className="mt-2 text-xs text-red-500" role="alert">
            Error: {message.error || 'Unknown error'}
          </div>
        )}
      </div>
      
      {/* Copy button */}
      <button
        onClick={onCopy}
        className={`absolute top-2 right-2 p-1 rounded-full bg-background/80 text-foreground opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2`}
        aria-label="Copy message content"
        title="Copy message"
      >
        <Copy className="h-3 w-3" aria-hidden="true" />
      </button>
    </div>
  );
};

export default ConversationDetail; 