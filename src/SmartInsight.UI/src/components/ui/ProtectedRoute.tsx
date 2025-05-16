import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import authService from '../../services/authService';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
}

/**
 * A wrapper component that protects routes requiring authentication.
 * It checks if the user is authenticated and has the required roles.
 * If not, it redirects to the login page with the original location stored in state.
 */
const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRoles = [] 
}) => {
  const location = useLocation();
  const isAuthenticated = authService.isAuthenticated();
  
  // If user is not authenticated, redirect to login
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  
  // If roles are required, check if user has at least one of them
  if (requiredRoles.length > 0) {
    const hasRequiredRole = requiredRoles.some(role => 
      authService.hasRole(role)
    );
    
    if (!hasRequiredRole) {
      // Redirect to unauthorized page or dashboard
      return <Navigate to="/unauthorized" replace />;
    }
  }
  
  // If all checks pass, render the protected content
  return <>{children}</>;
};

export default ProtectedRoute; 