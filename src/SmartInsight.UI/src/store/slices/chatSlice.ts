// Define the chat state type
export interface ChatState {
  conversations: Conversation[];
  activeConversationId: string | null;
  isLoading: boolean;
  error: string | null;
}

// Define conversation type
export interface Conversation {
  id: string;
  title: string;
  messages: Message[];
  createdAt: number;
  updatedAt: number;
}

// Define message type
export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: number;
  status: 'sending' | 'sent' | 'error';
  error?: string;
  metadata?: {
    executedQuery?: string;
    visualizationId?: string;
    datasetId?: string;
    [key: string]: any;
  };
}

// Define action types
export const CHAT_ACTIONS = {
  FETCH_CONVERSATIONS_REQUEST: 'chat/fetchConversationsRequest',
  FETCH_CONVERSATIONS_SUCCESS: 'chat/fetchConversationsSuccess',
  FETCH_CONVERSATIONS_FAILURE: 'chat/fetchConversationsFailure',
  
  CREATE_CONVERSATION: 'chat/createConversation',
  UPDATE_CONVERSATION: 'chat/updateConversation',
  DELETE_CONVERSATION: 'chat/deleteConversation',
  
  SET_ACTIVE_CONVERSATION: 'chat/setActiveConversation',
  
  SEND_MESSAGE_REQUEST: 'chat/sendMessageRequest',
  SEND_MESSAGE_SUCCESS: 'chat/sendMessageSuccess',
  SEND_MESSAGE_FAILURE: 'chat/sendMessageFailure',
  
  ADD_MESSAGE: 'chat/addMessage',
  UPDATE_MESSAGE: 'chat/updateMessage',
  DELETE_MESSAGE: 'chat/deleteMessage',
  
  CLEAR_CONVERSATION_HISTORY: 'chat/clearConversationHistory',
  CLEAR_ERROR: 'chat/clearError',
} as const;

// Define action interfaces
export type ChatAction =
  | { type: typeof CHAT_ACTIONS.FETCH_CONVERSATIONS_REQUEST }
  | { type: typeof CHAT_ACTIONS.FETCH_CONVERSATIONS_SUCCESS; payload: Conversation[] }
  | { type: typeof CHAT_ACTIONS.FETCH_CONVERSATIONS_FAILURE; payload: string }
  
  | { type: typeof CHAT_ACTIONS.CREATE_CONVERSATION; payload: Conversation }
  | { type: typeof CHAT_ACTIONS.UPDATE_CONVERSATION; payload: Conversation }
  | { type: typeof CHAT_ACTIONS.DELETE_CONVERSATION; payload: string }
  
  | { type: typeof CHAT_ACTIONS.SET_ACTIVE_CONVERSATION; payload: string | null }
  
  | { type: typeof CHAT_ACTIONS.SEND_MESSAGE_REQUEST; payload: { conversationId: string; message: Omit<Message, 'status'> } }
  | { type: typeof CHAT_ACTIONS.SEND_MESSAGE_SUCCESS; payload: { conversationId: string; messageId: string } }
  | { type: typeof CHAT_ACTIONS.SEND_MESSAGE_FAILURE; payload: { conversationId: string; messageId: string; error: string } }
  
  | { type: typeof CHAT_ACTIONS.ADD_MESSAGE; payload: { conversationId: string; message: Message } }
  | { type: typeof CHAT_ACTIONS.UPDATE_MESSAGE; payload: { conversationId: string; messageId: string; updates: Partial<Message> } }
  | { type: typeof CHAT_ACTIONS.DELETE_MESSAGE; payload: { conversationId: string; messageId: string } }
  
  | { type: typeof CHAT_ACTIONS.CLEAR_CONVERSATION_HISTORY }
  | { type: typeof CHAT_ACTIONS.CLEAR_ERROR };

// Initial state
const initialState: ChatState = {
  conversations: [],
  activeConversationId: null,
  isLoading: false,
  error: null,
};

// Create the reducer
export function chatReducer(state: ChatState = initialState, action: ChatAction): ChatState {
  switch (action.type) {
    case CHAT_ACTIONS.FETCH_CONVERSATIONS_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case CHAT_ACTIONS.FETCH_CONVERSATIONS_SUCCESS:
      return {
        ...state,
        conversations: action.payload,
        isLoading: false,
        error: null,
      };
    case CHAT_ACTIONS.FETCH_CONVERSATIONS_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
      
    case CHAT_ACTIONS.CREATE_CONVERSATION:
      return {
        ...state,
        conversations: [...state.conversations, action.payload],
        activeConversationId: action.payload.id,
      };
    case CHAT_ACTIONS.UPDATE_CONVERSATION:
      return {
        ...state,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.id
            ? { ...action.payload }
            : conversation
        ),
      };
    case CHAT_ACTIONS.DELETE_CONVERSATION:
      return {
        ...state,
        conversations: state.conversations.filter(
          (conversation) => conversation.id !== action.payload
        ),
        activeConversationId:
          state.activeConversationId === action.payload
            ? state.conversations.length > 1
              ? state.conversations.find((c) => c.id !== action.payload)?.id ?? null
              : null
            : state.activeConversationId,
      };
      
    case CHAT_ACTIONS.SET_ACTIVE_CONVERSATION:
      return {
        ...state,
        activeConversationId: action.payload,
      };
      
    case CHAT_ACTIONS.SEND_MESSAGE_REQUEST:
      const { conversationId, message } = action.payload;
      return {
        ...state,
        isLoading: true,
        conversations: state.conversations.map((conversation) =>
          conversation.id === conversationId
            ? {
                ...conversation,
                messages: [
                  ...conversation.messages,
                  { ...message, status: 'sending', id: message.id || crypto.randomUUID() } as Message,
                ],
                updatedAt: Date.now(),
              }
            : conversation
        ),
      };
    case CHAT_ACTIONS.SEND_MESSAGE_SUCCESS:
      return {
        ...state,
        isLoading: false,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.conversationId
            ? {
                ...conversation,
                messages: conversation.messages.map((message) =>
                  message.id === action.payload.messageId
                    ? { ...message, status: 'sent' }
                    : message
                ),
              }
            : conversation
        ),
      };
    case CHAT_ACTIONS.SEND_MESSAGE_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload.error,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.conversationId
            ? {
                ...conversation,
                messages: conversation.messages.map((message) =>
                  message.id === action.payload.messageId
                    ? { ...message, status: 'error', error: action.payload.error }
                    : message
                ),
              }
            : conversation
        ),
      };
      
    case CHAT_ACTIONS.ADD_MESSAGE:
      return {
        ...state,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.conversationId
            ? {
                ...conversation,
                messages: [...conversation.messages, action.payload.message],
                updatedAt: Date.now(),
              }
            : conversation
        ),
      };
    case CHAT_ACTIONS.UPDATE_MESSAGE:
      return {
        ...state,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.conversationId
            ? {
                ...conversation,
                messages: conversation.messages.map((message) =>
                  message.id === action.payload.messageId
                    ? { ...message, ...action.payload.updates }
                    : message
                ),
              }
            : conversation
        ),
      };
    case CHAT_ACTIONS.DELETE_MESSAGE:
      return {
        ...state,
        conversations: state.conversations.map((conversation) =>
          conversation.id === action.payload.conversationId
            ? {
                ...conversation,
                messages: conversation.messages.filter(
                  (message) => message.id !== action.payload.messageId
                ),
              }
            : conversation
        ),
      };
      
    case CHAT_ACTIONS.CLEAR_CONVERSATION_HISTORY:
      return {
        ...state,
        conversations: [],
        activeConversationId: null,
      };
    case CHAT_ACTIONS.CLEAR_ERROR:
      return {
        ...state,
        error: null,
      };
    default:
      return state;
  }
}

// Action creators
export const chatActions = {
  fetchConversationsRequest: (): ChatAction => ({
    type: CHAT_ACTIONS.FETCH_CONVERSATIONS_REQUEST,
  }),
  fetchConversationsSuccess: (conversations: Conversation[]): ChatAction => ({
    type: CHAT_ACTIONS.FETCH_CONVERSATIONS_SUCCESS,
    payload: conversations,
  }),
  fetchConversationsFailure: (error: string): ChatAction => ({
    type: CHAT_ACTIONS.FETCH_CONVERSATIONS_FAILURE,
    payload: error,
  }),
  
  createConversation: (conversation: Conversation): ChatAction => ({
    type: CHAT_ACTIONS.CREATE_CONVERSATION,
    payload: conversation,
  }),
  updateConversation: (conversation: Conversation): ChatAction => ({
    type: CHAT_ACTIONS.UPDATE_CONVERSATION,
    payload: conversation,
  }),
  deleteConversation: (conversationId: string): ChatAction => ({
    type: CHAT_ACTIONS.DELETE_CONVERSATION,
    payload: conversationId,
  }),
  
  setActiveConversation: (conversationId: string | null): ChatAction => ({
    type: CHAT_ACTIONS.SET_ACTIVE_CONVERSATION,
    payload: conversationId,
  }),
  
  sendMessageRequest: (
    conversationId: string,
    message: Omit<Message, 'status'>
  ): ChatAction => ({
    type: CHAT_ACTIONS.SEND_MESSAGE_REQUEST,
    payload: { conversationId, message },
  }),
  sendMessageSuccess: (
    conversationId: string,
    messageId: string
  ): ChatAction => ({
    type: CHAT_ACTIONS.SEND_MESSAGE_SUCCESS,
    payload: { conversationId, messageId },
  }),
  sendMessageFailure: (
    conversationId: string,
    messageId: string,
    error: string
  ): ChatAction => ({
    type: CHAT_ACTIONS.SEND_MESSAGE_FAILURE,
    payload: { conversationId, messageId, error },
  }),
  
  addMessage: (conversationId: string, message: Message): ChatAction => ({
    type: CHAT_ACTIONS.ADD_MESSAGE,
    payload: { conversationId, message },
  }),
  updateMessage: (
    conversationId: string,
    messageId: string,
    updates: Partial<Message>
  ): ChatAction => ({
    type: CHAT_ACTIONS.UPDATE_MESSAGE,
    payload: { conversationId, messageId, updates },
  }),
  deleteMessage: (conversationId: string, messageId: string): ChatAction => ({
    type: CHAT_ACTIONS.DELETE_MESSAGE,
    payload: { conversationId, messageId },
  }),
  
  clearConversationHistory: (): ChatAction => ({
    type: CHAT_ACTIONS.CLEAR_CONVERSATION_HISTORY,
  }),
  clearError: (): ChatAction => ({
    type: CHAT_ACTIONS.CLEAR_ERROR,
  }),
}; 