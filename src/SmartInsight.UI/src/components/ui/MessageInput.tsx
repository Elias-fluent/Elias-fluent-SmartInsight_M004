import React, { useState, useRef, useEffect } from 'react';
import type { KeyboardEvent } from 'react';
import { Send } from 'lucide-react';
import { useChat } from '../../hooks/useChat';
import { CHAT_ACTIONS } from '../../store/slices/chatSlice';
import type { Message } from '../../store/slices/chatSlice';

interface MessageInputProps {
  className?: string;
  placeholder?: string;
  onSend?: (message: string) => void;
}

const MessageInput: React.FC<MessageInputProps> = ({
  className = '',
  placeholder = 'Type your message...',
  onSend
}) => {
  const [message, setMessage] = useState('');
  const { dispatch, chat, createConversation, sendMessage } = useChat();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  
  // Auto-adjust textarea height
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = '0';
      const scrollHeight = textareaRef.current.scrollHeight;
      textareaRef.current.style.height = Math.min(scrollHeight, 150) + 'px';
    }
  }, [message]);

  const handleSendMessage = () => {
    if (!message.trim()) return;
    
    const conversationId = chat.activeConversationId;
    
    // If no active conversation, create one
    if (!conversationId) {
      const newConversationId = createConversation('New Conversation');
      
      // Send the message in the new conversation
      sendMessageToConversation(newConversationId);
    } else {
      // Send to existing conversation
      sendMessageToConversation(conversationId);
    }
  };
  
  const sendMessageToConversation = (conversationId: string) => {
    // Send the message
    sendMessage(conversationId, message, 'user');
    
    // Call the onSend handler if provided
    if (onSend) {
      onSend(message);
    }
    
    // Clear the input
    setMessage('');
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  return (
    <div className={`flex items-end border-t p-3 ${className}`}>
      <div className="flex-1 relative">
        <textarea
          ref={textareaRef}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          rows={1}
          className="resize-none w-full py-2 px-3 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          data-testid="message-input"
          aria-label="Message input"
        />
      </div>
      <button
        onClick={handleSendMessage}
        disabled={!message.trim()}
        className="ml-2 p-2 rounded-full bg-primary text-primary-foreground disabled:opacity-50 disabled:cursor-not-allowed"
        data-testid="send-button"
        aria-label="Send message"
      >
        <Send size={20} />
      </button>
    </div>
  );
};

export default MessageInput; 