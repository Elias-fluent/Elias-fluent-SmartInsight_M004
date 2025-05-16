import React, { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useStore } from '../../store/StoreContext';
import authService from '../../services/authService';
import TenantSelector from '../ui/TenantSelector';
import { authActions } from '../../store/slices/authSlice';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const { state, dispatch } = useStore();
  const navigate = useNavigate();
  
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
  
  return (
    <div className="min-h-screen bg-background flex flex-col">
      <header className="border-b border-border py-4">
        <div className="container mx-auto px-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-primary">SmartInsight</h1>
          
          <div className="flex items-center space-x-4">
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
              >
                Login
              </Link>
            )}
          </div>
        </div>
      </header>
      
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