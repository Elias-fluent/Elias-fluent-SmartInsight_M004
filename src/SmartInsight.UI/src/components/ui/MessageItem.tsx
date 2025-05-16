import React, { useState } from 'react';
import type { Message } from '../../store/slices/chatSlice';
import { useChat } from '../../store/StoreContext';
import { Check, CopyIcon, AlertCircle } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import remarkGfm from 'remark-gfm';
import type { Components } from 'react-markdown';
import MessageActions from './MessageActions';

interface MessageItemProps {
  message: Message;
}

const MessageItem: React.FC<MessageItemProps> = ({ message }) => {
  const { dispatch, chat } = useChat();
  const [copied, setCopied] = useState(false);
  
  const handleCopy = () => {
    navigator.clipboard.writeText(message.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  
  // Determine message style based on sender
  const isUser = message.role === 'user';
  const messageStyles = isUser 
    ? 'bg-primary text-primary-foreground ml-12'
    : 'bg-muted text-muted-foreground mr-12';
    
  const statusIcon = () => {
    switch (message.status) {
      case 'sending':
        return <div className="w-4 h-4 animate-spin rounded-full border-2 border-muted-foreground border-t-transparent" />;
      case 'error':
        return <AlertCircle className="w-4 h-4 text-destructive" />;
      default:
        return null;
    }
  };

  // Custom components for markdown
  const markdownComponents: Components = {
    // Add styles to the links
    a: (props) => (
      <a
        {...props}
        target="_blank"
        rel="noopener noreferrer"
        className="text-blue-600 dark:text-blue-400 hover:underline"
      />
    ),
    // Add styles to code blocks
    code: ({ className, children, ...props }: any) => {
      const match = /language-(\w+)/.exec(className || '');
      const isInline = !match && (props.inline as boolean);
      
      return (
        <code
          className={`${
            isInline
              ? 'bg-muted px-1 py-0.5 rounded text-sm'
              : 'block bg-muted p-2 rounded-md overflow-x-auto text-sm'
          } ${className || ''}`}
          {...props}
        >
          {children}
        </code>
      );
    },
    // Add styles to tables
    table: ({ children, ...props }) => (
      <div className="overflow-x-auto my-4">
        <table className="min-w-full divide-y divide-gray-300 dark:divide-gray-700" {...props}>
          {children}
        </table>
      </div>
    ),
    th: ({ children, ...props }) => (
      <th
        className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wider bg-muted-foreground/10"
        {...props}
      >
        {children}
      </th>
    ),
    td: ({ children, ...props }) => (
      <td className="px-3 py-2 text-sm" {...props}>
        {children}
      </td>
    ),
  };

  return (
    <div 
      className={`relative group rounded-lg p-3 ${messageStyles}`}
      data-testid={`message-${message.id}`}
    >
      <div className="flex justify-between items-start">
        <div className="font-medium text-xs mb-1">
          {isUser ? 'You' : 'Assistant'}
        </div>
        <div className="flex items-center space-x-2">
          {statusIcon()}
          <button 
            onClick={handleCopy} 
            className="opacity-0 group-hover:opacity-100 transition-opacity p-1 rounded hover:bg-muted/50"
            aria-label="Copy message"
          >
            {copied ? <Check size={16} /> : <CopyIcon size={16} />}
          </button>
          {chat.activeConversationId && (
            <MessageActions 
              message={message} 
              conversationId={chat.activeConversationId} 
            />
          )}
        </div>
      </div>
      
      <div className="whitespace-pre-wrap break-words prose dark:prose-invert prose-sm max-w-none">
        {isUser ? (
          <p>{message.content}</p>
        ) : (
          <ReactMarkdown
            components={markdownComponents}
            rehypePlugins={[rehypeSanitize]}
            remarkPlugins={[remarkGfm]}
          >
            {message.content}
          </ReactMarkdown>
        )}
      </div>
      
      {message.error && (
        <div className="mt-2 text-sm text-destructive">
          Error: {message.error}
        </div>
      )}
      
      <div className="text-xs text-right mt-1 opacity-70">
        {new Date(message.timestamp).toLocaleTimeString()}
      </div>
    </div>
  );
};

export default MessageItem; 