import React from 'react';
import type { Conversation, Message } from '../../store/slices/chatSlice';
import { Clock, Copy, Download, Share2, Trash2 } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import remarkGfm from 'remark-gfm';

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
  // Function to copy message content to clipboard
  const copyMessageToClipboard = (content: string) => {
    navigator.clipboard.writeText(content);
    // In a real app, show a notification
    console.log('Copied to clipboard');
  };

  // Format timestamp to readable format
  const formatTimestamp = (timestamp: number): string => {
    const date = new Date(timestamp);
    return date.toLocaleString();
  };

  if (!conversation) {
    return (
      <div className={`flex flex-col items-center justify-center h-full ${className}`}>
        <p className="text-lg text-muted-foreground mb-2">No conversation selected</p>
        <p className="text-sm text-muted-foreground">Select a conversation from the list to view its details</p>
      </div>
    );
  }

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* Conversation header */}
      <div className="flex justify-between items-center pb-3 border-b mb-4">
        <div>
          <h2 className="text-xl font-bold">
            {conversation.title || 'Untitled conversation'}
          </h2>
          <div className="flex items-center text-xs text-muted-foreground mt-1">
            <Clock className="h-3 w-3 mr-1" />
            <span>{formatTimestamp(conversation.createdAt)}</span>
          </div>
        </div>
        
        {/* Action buttons */}
        <div className="flex space-x-2">
          {onShare && (
            <button
              onClick={() => onShare(conversation)}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground"
              title="Share conversation"
            >
              <Share2 className="h-4 w-4" />
            </button>
          )}
          
          {onExport && (
            <button
              onClick={() => onExport(conversation)}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground"
              title="Export conversation"
            >
              <Download className="h-4 w-4" />
            </button>
          )}
          
          {onDelete && (
            <button
              onClick={() => onDelete(conversation.id)}
              className="p-2 rounded-full hover:bg-accent hover:text-accent-foreground"
              title="Delete conversation"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>
      
      {/* Conversation messages */}
      <div className="flex-1 overflow-y-auto space-y-6 pr-1">
        {conversation.messages.map((message) => (
          <MessageItem
            key={message.id}
            message={message}
            onCopy={() => copyMessageToClipboard(message.content)}
          />
        ))}
      </div>
    </div>
  );
};

// Message item component
interface MessageItemProps {
  message: Message;
  onCopy: () => void;
}

const MessageItem: React.FC<MessageItemProps> = ({ message, onCopy }) => {
  const isUser = message.role === 'user';
  
  return (
    <div className={`relative group ${isUser ? 'ml-10' : 'mr-10'}`}>
      <div
        className={`p-4 rounded-lg ${
          isUser
            ? 'bg-primary text-primary-foreground'
            : 'bg-muted text-foreground'
        }`}
      >
        <div className="flex justify-between items-start mb-2">
          <div className="font-medium">
            {isUser ? 'You' : 'Assistant'}
          </div>
          <div className="text-xs opacity-70">
            {new Date(message.timestamp).toLocaleTimeString()}
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
          <div className="mt-2 text-xs text-red-500">
            Error: {message.error || 'Unknown error'}
          </div>
        )}
      </div>
      
      {/* Copy button */}
      <button
        onClick={onCopy}
        className="absolute top-2 right-2 p-1 rounded-full bg-background/80 text-foreground opacity-0 group-hover:opacity-100 transition-opacity"
        title="Copy message"
      >
        <Copy className="h-3 w-3" />
      </button>
    </div>
  );
};

export default ConversationDetail; 