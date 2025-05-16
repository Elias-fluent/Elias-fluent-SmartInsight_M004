import React, { useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import type { Conversation } from '../../store/slices/chatSlice';
import { ChevronDown, ChevronUp, Search, X } from 'lucide-react';
import { useFocusVisible } from '../../hooks/useFocusVisible';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import { useAnnounce } from '../../hooks/useAnnounce';

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
  const { announce } = useAnnounce();
  const listContainerRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const { isFocusVisible } = useFocusVisible();

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

  // Setup keyboard navigation for the conversation list
  const { focusedIndex, handleKeyDown, setFocusedIndex } = useKeyboardNavigation({
    itemCount: filteredConversations.length,
    direction: 'vertical',
    wrap: true,
    containerRef: listContainerRef as React.RefObject<HTMLElement>,
    itemSelector: '[role="option"]',
    onSelect: (index) => {
      const conversation = filteredConversations[index];
      if (conversation) {
        onSelectConversation(conversation.id);
        announce(`Selected conversation: ${conversation.title || 'Untitled conversation'}`);
      }
    }
  });

  // Toggle sort order
  const toggleSortOrder = () => {
    const newOrder = sortOrder === 'newest' ? 'oldest' : 'newest';
    setSortOrder(newOrder);
    announce(`Conversations sorted by ${newOrder === 'newest' ? 'newest first' : 'oldest first'}`);
  };

  // Handle clearing the search query
  const clearSearchQuery = () => {
    setSearchQuery('');
    announce('Search cleared');
    searchInputRef.current?.focus();
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

  // Handle conversation list container keyboard events
  const handleListKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    handleKeyDown(e);
  };

  // Set focused index when mouse hovers over a conversation
  const handleConversationHover = (index: number) => {
    setFocusedIndex(index);
  };

  return (
    <div 
      className={`flex flex-col h-full ${className}`}
      role="region"
      aria-label="Conversations"
    >
      {/* Search and sort controls */}
      <div className="flex flex-col space-y-2 mb-3" role="search" aria-label="Search conversations">
        <div className="relative">
          <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" aria-hidden="true" />
          <input
            ref={searchInputRef}
            type="text"
            placeholder="Search conversations..."
            className="w-full pl-8 pr-3 py-2 text-sm bg-background border rounded focus:outline-none focus:ring-1 focus:ring-primary"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            aria-label="Search conversations"
            role="searchbox"
          />
          {searchQuery && (
            <button
              className="absolute right-2 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1"
              onClick={clearSearchQuery}
              aria-label="Clear search"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          )}
        </div>
        
        <button
          className="flex items-center text-xs text-muted-foreground hover:text-foreground self-end focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          onClick={toggleSortOrder}
          aria-label={`Sort by ${sortOrder === 'newest' ? 'oldest' : 'newest'} first`}
          aria-pressed={sortOrder === 'newest'}
        >
          Sort: {sortOrder === 'newest' ? 'Newest first' : 'Oldest first'}
          {sortOrder === 'newest' ? (
            <ChevronDown className="ml-1 h-3 w-3" aria-hidden="true" />
          ) : (
            <ChevronUp className="ml-1 h-3 w-3" aria-hidden="true" />
          )}
        </button>
      </div>

      {/* List of conversations */}
      <div 
        className="flex-1 overflow-y-auto" 
        ref={listContainerRef}
        onKeyDown={handleListKeyDown}
        role="listbox"
        aria-label="Conversation list"
        tabIndex={filteredConversations.length > 0 ? 0 : undefined}
        aria-activedescendant={
          focusedIndex >= 0 && filteredConversations.length > 0
            ? `conversation-${filteredConversations[focusedIndex]?.id}`
            : undefined
        }
      >
        {filteredConversations.length === 0 ? (
          <div 
            className="text-center py-8 text-muted-foreground"
            role="status"
            aria-live="polite"
          >
            {searchQuery ? 'No matching conversations found' : 'No conversations yet'}
          </div>
        ) : (
          <div className="space-y-2">
            {filteredConversations.map((conversation, index) => (
              <div
                key={conversation.id}
                id={`conversation-${conversation.id}`}
                className={`p-3 rounded border transition-colors cursor-pointer hover:bg-accent hover:text-accent-foreground ${
                  conversation.id === activeConversationId
                    ? 'bg-accent text-accent-foreground'
                    : 'bg-card text-card-foreground'
                } ${isFocusVisible && focusedIndex === index ? 'ring-2 ring-ring ring-offset-2' : ''}`}
                onClick={() => onSelectConversation(conversation.id)}
                onMouseEnter={() => handleConversationHover(index)}
                role="option"
                aria-selected={conversation.id === activeConversationId}
                tabIndex={focusedIndex === index ? 0 : -1}
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
                    <time dateTime={new Date(conversation.updatedAt).toISOString()}>
                      {new Date(conversation.updatedAt).toLocaleDateString()}
                    </time>
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