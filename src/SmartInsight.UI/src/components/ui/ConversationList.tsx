import React, { useState } from 'react';
import type { Conversation } from '../../store/slices/chatSlice';
import { ChevronDown, ChevronUp, Search } from 'lucide-react';

interface ConversationListProps {
  conversations: Conversation[];
  activeConversationId: string | null;
  onSelectConversation: (id: string) => void;
  className?: string;
}

const ConversationList: React.FC<ConversationListProps> = ({
  conversations,
  activeConversationId,
  onSelectConversation,
  className = '',
}) => {
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest');
  const [searchQuery, setSearchQuery] = useState('');

  // Sort conversations based on the selected order
  const sortedConversations = [...conversations].sort((a, b) => {
    return sortOrder === 'newest'
      ? b.updatedAt - a.updatedAt
      : a.updatedAt - b.updatedAt;
  });

  // Filter conversations based on search query
  const filteredConversations = sortedConversations.filter((conversation) => {
    if (!searchQuery) return true;
    
    const query = searchQuery.toLowerCase();
    
    // Search in title
    if (conversation.title?.toLowerCase().includes(query)) return true;
    
    // Search in messages
    return conversation.messages.some((message) => 
      message.content.toLowerCase().includes(query)
    );
  });

  // Toggle sort order
  const toggleSortOrder = () => {
    setSortOrder(sortOrder === 'newest' ? 'oldest' : 'newest');
  };

  // Get conversation preview (first few words of the first message)
  const getConversationPreview = (conversation: Conversation): string => {
    if (conversation.messages.length === 0) return 'No messages';
    
    const firstMessage = conversation.messages[0];
    const preview = firstMessage.content.trim().split(' ').slice(0, 5).join(' ');
    
    return preview.length < firstMessage.content.length 
      ? `${preview}...` 
      : preview;
  };

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* Search and sort controls */}
      <div className="flex flex-col space-y-2 mb-3">
        <div className="relative">
          <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
          <input
            type="text"
            placeholder="Search conversations..."
            className="w-full pl-8 pr-3 py-2 text-sm bg-background border rounded focus:outline-none focus:ring-1 focus:ring-primary"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          {searchQuery && (
            <button
              className="absolute right-2 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground"
              onClick={() => setSearchQuery('')}
            >
              ×
            </button>
          )}
        </div>
        
        <button
          className="flex items-center text-xs text-muted-foreground hover:text-foreground self-end"
          onClick={toggleSortOrder}
        >
          Sort: {sortOrder === 'newest' ? 'Newest first' : 'Oldest first'}
          {sortOrder === 'newest' ? (
            <ChevronDown className="ml-1 h-3 w-3" />
          ) : (
            <ChevronUp className="ml-1 h-3 w-3" />
          )}
        </button>
      </div>

      {/* List of conversations */}
      <div className="flex-1 overflow-y-auto">
        {filteredConversations.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            {searchQuery ? 'No matching conversations found' : 'No conversations yet'}
          </div>
        ) : (
          <div className="space-y-2">
            {filteredConversations.map((conversation) => (
              <div
                key={conversation.id}
                className={`p-3 rounded border transition-colors cursor-pointer hover:bg-accent hover:text-accent-foreground ${
                  conversation.id === activeConversationId
                    ? 'bg-accent text-accent-foreground'
                    : 'bg-card text-card-foreground'
                }`}
                onClick={() => onSelectConversation(conversation.id)}
              >
                <div className="font-medium truncate">
                  {conversation.title || 'Untitled conversation'}
                </div>
                <div className="text-xs mt-1 text-muted-foreground truncate">
                  {getConversationPreview(conversation)}
                </div>
                <div className="flex justify-between items-center text-xs mt-2">
                  <span className="text-muted-foreground">
                    {conversation.messages.length} message{conversation.messages.length !== 1 ? 's' : ''}
                  </span>
                  <span className="text-muted-foreground">
                    {new Date(conversation.updatedAt).toLocaleDateString()}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default ConversationList; 