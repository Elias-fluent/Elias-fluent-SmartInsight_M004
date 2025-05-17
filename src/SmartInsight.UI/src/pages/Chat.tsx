import React, { useEffect, useState } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import type { RootState } from '../store/configureStore';
import { chatActions } from '../store/slices/chatSlice';
import ChatContainer from '../components/ui/ChatContainer';
import MessageList from '../components/ui/MessageList';
import MessageInput from '../components/ui/MessageInput';
import TypingIndicator from '../components/ui/TypingIndicator';
import { v4 as uuid } from 'uuid';

const Chat: React.FC = () => {
  const chat = useSelector((state: RootState) => state.chat);
  const dispatch = useDispatch();
  const [isTyping, setIsTyping] = useState(false);
  
  // Simulate assistant typing when new user message is sent
  useEffect(() => {
    // Find the active conversation
    const activeConversation = chat.conversations.find(
      (conv) => conv.id === chat.activeConversationId
    );
    
    if (!activeConversation) return;
    
    // Get the last message
    const lastMessage = activeConversation.messages[activeConversation.messages.length - 1];
    
    // If the last message is from the user and status is sent, simulate assistant reply
    if (lastMessage && lastMessage.role === 'user' && lastMessage.status === 'sent') {
      // Start typing indicator
      setIsTyping(true);
      
      // Simulate a delay before the assistant responds
      const typingTimeout = setTimeout(() => {
        setIsTyping(false);
        
        // Create assistant response
        const messageId = uuid();
        const assistantMessage = {
          id: messageId,
          role: 'assistant' as const,
          content: simulateAssistantResponse(lastMessage.content),
          timestamp: Date.now(),
          status: 'sent' as const
        };
        
        // Add the assistant's response
        dispatch(
          chatActions.addMessage(chat.activeConversationId as string, assistantMessage)
        );
      }, 1500);
      
      return () => clearTimeout(typingTimeout);
    }
  }, [chat.conversations, chat.activeConversationId, dispatch]);

  // Function to handle new message sending
  const handleSendMessage = (message: string) => {
    // The MessageInput component handles the actual sending
    console.log('Message sent:', message);
  };

  // Function to create a simulated response from the assistant
  // In a real implementation, this would be replaced with an API call
  const simulateAssistantResponse = (userMessage: string): string => {
    // Simple simulation for demo purposes
    if (userMessage.toLowerCase().includes('hello') || userMessage.toLowerCase().includes('hi')) {
      return 'Hello! How can I help you today?';
    }
    
    if (userMessage.toLowerCase().includes('help')) {
      return "I'm here to help! You can ask me about:\n\n- Analyzing your data\n- Finding information in your documents\n- Generating reports\n- Answering questions about your business";
    }
    
    if (userMessage.toLowerCase().includes('data') || userMessage.toLowerCase().includes('report')) {
      return "Here's some sample data analysis:\n\n```sql\nSELECT department, COUNT(*) as employee_count, AVG(salary) as avg_salary\nFROM employees\nGROUP BY department\nORDER BY employee_count DESC;\n```\n\nThis query would show you the number of employees and average salary by department, sorted by the largest departments first.";
    }
    
    // Fallback response
    return "I understand your message. As this is a demonstration, I'm providing a simulated response. In the production version, I would connect to the AI backend to provide relevant information based on your organization's knowledge base.";
  };

  return (
    <div className="h-[calc(100vh-4rem)] p-4">
      <ChatContainer>
        <MessageList />
        <TypingIndicator isTyping={isTyping} />
        <MessageInput onSend={handleSendMessage} />
      </ChatContainer>
    </div>
  );
};

export default Chat; 