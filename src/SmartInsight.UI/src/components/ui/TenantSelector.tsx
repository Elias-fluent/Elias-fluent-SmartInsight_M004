import React, { useState, useEffect } from 'react';
import { useStore } from '../../store/StoreContext';
import { authActions } from '../../store/slices/authSlice';
import authService from '../../services/authService';

interface Tenant {
  id: string;
  name: string;
}

interface TenantSelectorProps {
  className?: string;
}

const TenantSelector: React.FC<TenantSelectorProps> = ({ className = '' }) => {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState<string | undefined>(
    authService.getCurrentTenantId()
  );
  const [isLoading, setIsLoading] = useState(false);
  
  const { dispatch } = useStore();
  
  // Fetch tenants on component mount
  useEffect(() => {
    const fetchTenants = async () => {
      setIsLoading(true);
      try {
        // In a real app, this would be an API call to get available tenants
        // For now, we'll mock some tenants
        const mockTenants: Tenant[] = [
          { id: '1', name: 'Tenant 1' },
          { id: '2', name: 'Tenant 2' },
          { id: '3', name: 'Tenant 3' }
        ];
        
        setTenants(mockTenants);
        
        // If no tenant is selected yet and we have tenants, select the first one
        if (!selectedTenantId && mockTenants.length > 0) {
          handleTenantChange(mockTenants[0].id);
        }
      } catch (error) {
        console.error('Failed to fetch tenants:', error);
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchTenants();
  }, [selectedTenantId]);
  
  const handleTenantChange = (tenantId: string) => {
    setSelectedTenantId(tenantId);
    
    // Update the Redux store
    dispatch(authActions.setTenant(tenantId));
  };
  
  if (isLoading) {
    return <div className={`${className} inline-block px-2 py-1`}>Loading tenants...</div>;
  }
  
  if (tenants.length === 0) {
    return <div className={`${className} inline-block px-2 py-1`}>No tenants available</div>;
  }
  
  return (
    <div className={`${className}`}>
      <select
        value={selectedTenantId}
        onChange={(e) => handleTenantChange(e.target.value)}
        className="block w-full px-3 py-2 text-base bg-white border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
      >
        <option value="" disabled>
          Select a tenant
        </option>
        {tenants.map((tenant) => (
          <option key={tenant.id} value={tenant.id}>
            {tenant.name}
          </option>
        ))}
      </select>
    </div>
  );
};

export default TenantSelector; 