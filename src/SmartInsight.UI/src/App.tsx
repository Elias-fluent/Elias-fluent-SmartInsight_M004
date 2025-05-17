import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import MainLayout from './components/layout/MainLayout';
import Dashboard from './pages/Dashboard';
import Chat from './pages/Chat';
import History from './pages/History';
import Admin from './pages/Admin';
import Login from './pages/Login';
import Unauthorized from './pages/Unauthorized';
import ProtectedRoute from './components/ui/ProtectedRoute';
import authService from './services/authService';

const App: React.FC = () => {
  // Initialize auth service interceptors
  useEffect(() => {
    authService.setupInterceptors();
  }, []);

  return (
    <Router>
      <MainLayout>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<Login />} />
          <Route path="/unauthorized" element={<Unauthorized />} />
          
          {/* Protected routes */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          
          <Route
            path="/chat"
            element={
              <ProtectedRoute>
                <Chat />
              </ProtectedRoute>
            }
          />
          
          <Route
            path="/history"
            element={
              <ProtectedRoute>
                <History />
              </ProtectedRoute>
            }
          />
          
          {/* Admin routes */}
          <Route
            path="/admin"
            element={
              <ProtectedRoute requiredRoles={['Admin']}>
                <Admin />
              </ProtectedRoute>
            }
          />
          
          {/* Default route - redirect to home */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </MainLayout>
    </Router>
  );
};

export default App;
