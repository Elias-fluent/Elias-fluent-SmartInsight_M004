// import React from 'react';
import MainLayout from './components/layout/MainLayout';
import Dashboard from './pages/Dashboard';
import { StoreProvider } from './store/StoreContext';

function App() {
  return (
    <StoreProvider>
      <MainLayout>
        <Dashboard />
      </MainLayout>
    </StoreProvider>
  );
}

export default App;
