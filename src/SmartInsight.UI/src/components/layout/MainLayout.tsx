import React, { useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useStore } from '../../store/StoreContext';
import authService from '../../services/authService';
import TenantSelector from '../ui/TenantSelector';
import AccessibilityButton from '../ui/AccessibilityModal';
import { authActions } from '../../store/slices/authSlice';
import { Home, MessageSquare, BarChart3, Settings, History } from 'lucide-react';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const { state, dispatch } = useStore();
  const navigate = useNavigate();
  const location = useLocation();
  
  // Get authentication state from our custom store
  const { isAuthenticated, user } = state.auth;
  
  // Setup auth interceptors
  useEffect(() => {
    authService.setupInterceptors();
  }, []);
  
  // Handle logout
  const handleLogout = () => {
    authService.logout();
    dispatch(authActions.logout());
    navigate('/login');
  };
  
  // Check if a path is active
  const isActive = (path: string) => {
    return location.pathname === path;
  };
  
  return (
    <div className="min-h-screen bg-background flex flex-col">
      <header className="border-b border-border py-4">
        <div className="container mx-auto px-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-primary">SmartInsight</h1>
          
          <div className="flex items-center space-x-4">
            {/* Accessibility Button - always visible */}
            <AccessibilityButton />
            
            {isAuthenticated ? (
              <>
                {/* Show tenant selector if authenticated */}
                <TenantSelector className="w-40" />
                
                {/* User menu */}
                <div className="relative">
                  <div className="flex items-center space-x-2 cursor-pointer">
                    <span className="text-sm font-medium">
                      {user?.username || 'User'}
                    </span>
                    <button
                      onClick={handleLogout}
                      className="px-3 py-1 text-sm text-white bg-red-600 rounded hover:bg-red-700"
                      aria-label="Logout from account"
                    >
                      Logout
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <Link
                to="/login"
                className="px-4 py-2 text-sm text-white bg-blue-600 rounded hover:bg-blue-700"
                aria-label="Log in to your account"
              >
                Login
              </Link>
            )}
          </div>
        </div>
      </header>
      
      {isAuthenticated && (
        <nav className="border-b border-border bg-muted/20" aria-label="Main navigation">
          <div className="container mx-auto px-4">
            <ul className="flex space-x-2">
              <li>
                <Link
                  to="/"
                  className={`flex items-center space-x-1 px-4 py-3 text-sm font-medium ${
                    isActive('/') ? 'text-primary border-b-2 border-primary' : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-current={isActive('/') ? 'page' : undefined}
                >
                  <Home size={18} aria-hidden="true" />
                  <span>Dashboard</span>
                </Link>
              </li>
              <li>
                <Link
                  to="/chat"
                  className={`flex items-center space-x-1 px-4 py-3 text-sm font-medium ${
                    isActive('/chat') ? 'text-primary border-b-2 border-primary' : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-current={isActive('/chat') ? 'page' : undefined}
                >
                  <MessageSquare size={18} aria-hidden="true" />
                  <span>Chat</span>
                </Link>
              </li>
              <li>
                <Link
                  to="/history"
                  className={`flex items-center space-x-1 px-4 py-3 text-sm font-medium ${
                    isActive('/history') ? 'text-primary border-b-2 border-primary' : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-current={isActive('/history') ? 'page' : undefined}
                >
                  <History size={18} aria-hidden="true" />
                  <span>History</span>
                </Link>
              </li>
              <li>
                <Link
                  to="/analytics"
                  className={`flex items-center space-x-1 px-4 py-3 text-sm font-medium ${
                    isActive('/analytics') ? 'text-primary border-b-2 border-primary' : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-current={isActive('/analytics') ? 'page' : undefined}
                >
                  <BarChart3 size={18} aria-hidden="true" />
                  <span>Analytics</span>
                </Link>
              </li>
              {user?.roles.includes('Admin') && (
                <li>
                  <Link
                    to="/admin"
                    className={`flex items-center space-x-1 px-4 py-3 text-sm font-medium ${
                      isActive('/admin') ? 'text-primary border-b-2 border-primary' : 'text-muted-foreground hover:text-foreground'
                    }`}
                    aria-current={isActive('/admin') ? 'page' : undefined}
                  >
                    <Settings size={18} aria-hidden="true" />
                    <span>Admin</span>
                  </Link>
                </li>
              )}
            </ul>
          </div>
        </nav>
      )}
      
      <main className="flex-1 container mx-auto px-4 py-6">
        {children}
      </main>
      
      <footer className="border-t border-border py-4 mt-auto">
        <div className="container mx-auto px-4 text-center text-sm text-muted-foreground">
          &copy; {new Date().getFullYear()} SmartInsight - All rights reserved
        </div>
      </footer>
    </div>
  );
};

export default MainLayout; 