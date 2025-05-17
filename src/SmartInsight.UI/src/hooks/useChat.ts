import { useSelector, useDispatch } from 'react-redux';
import type { RootState } from '../store/configureStore';
import { CHAT_ACTIONS } from '../store/slices/chatSlice';
import type { Conversation, Message } from '../store/slices/chatSlice';

/**
 * Hook for accessing chat state and actions
 */
export function useChat() {
  const chat = useSelector((state: RootState) => state.chat);
  const dispatch = useDispatch();

  // Helper to generate a unique ID
  const generateId = () => crypto.randomUUID();

  return {
    chat,
    dispatch,
    // Chat-specific action creators
    setActiveConversation: (conversationId: string | null) => {
      dispatch({ type: CHAT_ACTIONS.SET_ACTIVE_CONVERSATION, payload: conversationId });
    },
    addMessage: (conversationId: string, message: Message) => {
      dispatch({ 
        type: CHAT_ACTIONS.ADD_MESSAGE, 
        payload: { conversationId, message } 
      });
    },
    createConversation: (title: string) => {
      const newConversation: Conversation = {
        id: generateId(),
        title,
        messages: [],
        createdAt: Date.now(),
        updatedAt: Date.now()
      };
      dispatch({ 
        type: CHAT_ACTIONS.CREATE_CONVERSATION, 
        payload: newConversation 
      });
      return newConversation.id;
    },
    deleteConversation: (conversationId: string) => {
      dispatch({ 
        type: CHAT_ACTIONS.DELETE_CONVERSATION, 
        payload: conversationId 
      });
    },
    sendMessage: (conversationId: string, content: string, role: 'user' | 'assistant' | 'system' = 'user') => {
      const messageId = generateId();
      const message: Omit<Message, 'status'> = {
        id: messageId,
        role,
        content,
        timestamp: Date.now(),
        metadata: {}
      };
      
      dispatch({
        type: CHAT_ACTIONS.SEND_MESSAGE_REQUEST,
        payload: { conversationId, message }
      });
      
      return messageId;
    }
  };
} 