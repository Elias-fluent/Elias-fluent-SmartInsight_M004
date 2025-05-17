// Script to bypass authentication for testing UI without a working API
// Run this in the browser console to set mock authentication state

(function mockAuth() {
  // Create a mock user with admin role
  const mockUser = {
    id: '12345',
    username: 'admin@smartinsight.local',
    email: 'admin@smartinsight.local',
    firstName: 'Admin',
    lastName: 'User',
    roles: ['Admin', 'User'],
    tenantId: '67890'
  };

  // Create a mock JWT token (not real)
  const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NSIsImVtYWlsIjoiYWRtaW5Ac21hcnRpbnNpZ2h0LmxvY2FsIiwicm9sZSI6WyJBZG1pbiIsIlVzZXIiXSwidGVuYW50SWQiOiI2Nzg5MCIsImV4cCI6MTcwOTQ3MzE0MCwiaWF0IjoxNzA5MzkzMTQwfQ.mock_signature';

  // Create and save auth state for Redux store
  const authState = {
    isAuthenticated: true,
    user: mockUser,
    token: mockToken,
    loading: false,
    error: null,
    tenantId: mockUser.tenantId
  };

  // Save to localStorage using the correct keys
  localStorage.setItem('smartinsight_auth', JSON.stringify(authState));
  localStorage.setItem('smartinsight_token', mockToken);
  
  // Set UI state with default values for Redux store
  const uiState = {
    isLoading: false,
    notifications: [],
    theme: 'light',
    sidebarOpen: true,
    activeModal: null,
    currentView: 'dashboard',
    contrastMode: 'normal',
    colorBlindMode: 'normal',
    textSize: 'normal'
  };
  localStorage.setItem('smartinsight_ui', JSON.stringify(uiState));

  // Manually dispatch Redux actions to update state
  if (window.store && typeof window.store.dispatch === 'function') {
    window.store.dispatch({ 
      type: 'auth/loginSuccess', 
      payload: { user: mockUser, token: mockToken } 
    });
    
    window.store.dispatch({
      type: 'auth/setTenant',
      payload: mockUser.tenantId
    });
  } else {
    console.warn('Redux store not found on window object. State saved to localStorage only.');
    console.warn('You will need to refresh the page for changes to take effect.');
  }

  console.log('Authentication bypassed! Refresh the page to access protected routes.');
  console.log('You may need to manually navigate to the route you want to test.');
  console.log('You can now access routes like:');
  console.log('- / (Dashboard)');
  console.log('- /admin (Admin panel)');
  console.log('- /chat (Chat interface)');
})(); 