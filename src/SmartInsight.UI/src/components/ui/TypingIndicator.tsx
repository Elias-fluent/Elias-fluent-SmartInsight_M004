import React from 'react';

interface TypingIndicatorProps {
  isTyping: boolean;
  className?: string;
}

const TypingIndicator: React.FC<TypingIndicatorProps> = ({ 
  isTyping, 
  className = '' 
}) => {
  if (!isTyping) return null;

  return (
    <div 
      className={`flex items-center space-x-1 p-2 ${className}`}
      data-testid="typing-indicator"
      aria-live="polite"
      aria-label="Assistant is typing"
    >
      <div className="text-xs text-muted-foreground">Assistant is typing</div>
      <div className="flex space-x-1">
        <div className="w-1.5 h-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
        <div className="w-1.5 h-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
        <div className="w-1.5 h-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
      </div>
    </div>
  );
};

export default TypingIndicator; 