import React, { useEffect, useState } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { CHAT_ACTIONS } from '../store/slices/chatSlice';
import ConversationList from '../components/ui/ConversationList';
import ConversationDetail from '../components/ui/ConversationDetail';
import type { Conversation } from '../store/slices/chatSlice';
import { useNavigate } from 'react-router-dom';
import { PlusCircle } from 'lucide-react';
import type { RootState } from '../store/configureStore';

const History: React.FC = () => {
  const chat = useSelector((state: RootState) => state.chat);
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [conversationToDelete, setConversationToDelete] = useState<string | null>(null);

  // Fetch conversations when component mounts
  useEffect(() => {
    // In a real implementation, this would be replaced with an API call
    // For now we'll use whatever conversations are loaded in the store
    console.log('Fetching conversation history');
  }, [dispatch]);

  // Navigate to chat page with a new conversation
  const handleNewChat = () => {
    // Create a new conversation first
    const newConversation: Conversation = {
      id: crypto.randomUUID(),
      title: '',
      messages: [],
      createdAt: Date.now(),
      updatedAt: Date.now(),
    };
    
    dispatch({ type: CHAT_ACTIONS.CREATE_CONVERSATION, payload: newConversation });
    
    // Navigate to chat page
    navigate('/chat');
  };

  // Handle conversation selection
  const handleSelectConversation = (conversationId: string) => {
    dispatch({ type: CHAT_ACTIONS.SET_ACTIVE_CONVERSATION, payload: conversationId });
  };

  // Handle conversation export
  const handleExportConversation = (conversation: Conversation) => {
    // In a real implementation, this would generate a file for download
    console.log('Exporting conversation:', conversation.id);
    
    // Simulate export - in a real app, use proper export logic
    const exportData = JSON.stringify(conversation, null, 2);
    const blob = new Blob([exportData], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `conversation-${conversation.id}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  // Handle initiating conversation deletion
  const handleDeleteRequest = (conversationId: string) => {
    setConversationToDelete(conversationId);
    setShowDeleteModal(true);
  };

  // Handle confirming conversation deletion
  const handleConfirmDelete = () => {
    if (conversationToDelete) {
      dispatch({ type: CHAT_ACTIONS.DELETE_CONVERSATION, payload: conversationToDelete });
      setShowDeleteModal(false);
      setConversationToDelete(null);
    }
  };

  // Find the active conversation object based on ID
  const activeConversation = chat.activeConversationId 
    ? chat.conversations.find(c => c.id === chat.activeConversationId) || null
    : null;

  return (
    <div className="h-[calc(100vh-8rem)] flex flex-col space-y-4">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Conversation History</h1>
        <div className="flex items-center space-x-2">
          <button 
            className="flex items-center px-3 py-2 text-sm bg-primary text-primary-foreground rounded hover:bg-primary/90"
            onClick={handleNewChat}
          >
            <PlusCircle className="mr-2 h-4 w-4" />
            New Chat
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-4 flex-1 overflow-hidden">
        {/* Conversation list sidebar */}
        <div className="md:col-span-4 lg:col-span-3 border rounded-lg overflow-hidden bg-card">
          <ConversationList
            conversations={chat.conversations}
            activeConversationId={chat.activeConversationId}
            onSelectConversation={handleSelectConversation}
            className="p-4 h-full"
          />
        </div>
        
        {/* Conversation detail view */}
        <div className="md:col-span-8 lg:col-span-9 border rounded-lg overflow-hidden bg-card">
          <ConversationDetail
            conversation={activeConversation}
            onExport={handleExportConversation}
            onDelete={handleDeleteRequest}
            className="p-4 h-full"
          />
        </div>
      </div>

      {/* Delete confirmation modal */}
      {showDeleteModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-card p-6 rounded-lg max-w-md w-full">
            <h2 className="text-lg font-bold mb-4">Delete Conversation</h2>
            <p className="mb-6">Are you sure you want to delete this conversation? This action cannot be undone.</p>
            <div className="flex justify-end space-x-3">
              <button 
                className="px-4 py-2 bg-background border rounded-md"
                onClick={() => setShowDeleteModal(false)}
              >
                Cancel
              </button>
              <button 
                className="px-4 py-2 bg-red-600 text-white rounded-md"
                onClick={handleConfirmDelete}
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default History; 