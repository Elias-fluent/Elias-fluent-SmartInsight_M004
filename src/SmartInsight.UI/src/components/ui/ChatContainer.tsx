import React, { useRef, useEffect } from 'react';
import { useChat } from '../../store/StoreContext';
import { Card } from './card';

interface ChatContainerProps {
  children?: React.ReactNode;
  className?: string;
}

const ChatContainer: React.FC<ChatContainerProps> = ({ 
  children, 
  className = '' 
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const { chat } = useChat();
  
  // Find the active conversation
  const activeConversation = chat.conversations.find(
    (conv) => conv.id === chat.activeConversationId
  );

  // Scroll to bottom when new messages are added
  useEffect(() => {
    if (containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [activeConversation?.messages]);

  return (
    <Card className={`flex flex-col h-full ${className}`}>
      <div 
        ref={containerRef}
        className="flex-1 flex flex-col overflow-hidden"
        data-testid="chat-container"
      >
        {children}
      </div>
    </Card>
  );
};

export default ChatContainer; 