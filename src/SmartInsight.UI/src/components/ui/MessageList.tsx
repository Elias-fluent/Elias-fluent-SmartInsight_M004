import React from 'react';
import { useChat } from '../../store/StoreContext';
import type { Message } from '../../store/slices/chatSlice';
import MessageItem from './MessageItem';

interface MessageListProps {
  className?: string;
}

const MessageList: React.FC<MessageListProps> = ({ className = '' }) => {
  const { chat } = useChat();
  
  // Find the active conversation
  const activeConversation = chat.conversations.find(
    conv => conv.id === chat.activeConversationId
  );

  // If no active conversation, show empty state
  if (!activeConversation) {
    return (
      <div className={`flex-1 flex items-center justify-center p-4 ${className}`}>
        <div className="text-center text-gray-500 dark:text-gray-400">
          <p>No active conversation</p>
          <p className="text-sm">Start a new conversation to begin chatting</p>
        </div>
      </div>
    );
  }

  return (
    <div 
      className={`flex-1 overflow-y-auto p-4 space-y-4 ${className}`}
      data-testid="message-list"
    >
      {activeConversation.messages.length === 0 ? (
        <div className="text-center text-gray-500 dark:text-gray-400 py-8">
          <p>No messages yet</p>
          <p className="text-sm">Type a message to start the conversation</p>
        </div>
      ) : (
        activeConversation.messages.map((message: Message) => (
          <MessageItem key={message.id} message={message} />
        ))
      )}
    </div>
  );
};

export default MessageList; 