import React, { useEffect, useState } from 'react';
import { Button } from '../ui/button';
import { 
  Table, 
  TableBody, 
  TableCell, 
  TableHead, 
  TableHeader, 
  TableRow 
} from '../ui/table';
import { useToast } from '../ui/use-toast';
import { Plus, Edit, Trash2, RefreshCw, Database, Calendar, Settings, Shield } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';
import TenantForm from './TenantForm';
import DeleteConfirmation from './DeleteConfirmation';
import TenantConfig from './TenantConfig';
import TenantSecuritySettings from './TenantSecuritySettings';

interface Tenant {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
  dataSourceCount?: number;
  userCount?: number;
}

const TenantList: React.FC = () => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [tenantToDelete, setTenantToDelete] = useState<Tenant | null>(null);
  const [showConfig, setShowConfig] = useState(false);
  const [configuringTenantId, setConfiguringTenantId] = useState('');
  const [showSecuritySettings, setShowSecuritySettings] = useState(false);
  const [securitySettingsTenantId, setSecuritySettingsTenantId] = useState('');

  // Get tenants from Redux store (will need to be added to data slice)
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];

  // Fetch tenants on component mount
  useEffect(() => {
    fetchTenants();
  }, []);

  const fetchTenants = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/Tenants',
        'get',
        undefined,
        'FETCH_TENANTS_SUCCESS',
        'FETCH_TENANTS_FAILURE'
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch tenants',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddNew = () => {
    setEditingTenant(null);
    setShowForm(true);
  };

  const handleEdit = (tenant: Tenant) => {
    setEditingTenant(tenant);
    setShowForm(true);
  };

  const handleDelete = (tenant: Tenant) => {
    setTenantToDelete(tenant);
    setShowDeleteConfirm(true);
  };

  const confirmDelete = async () => {
    if (!tenantToDelete) return;

    try {
      await dispatch(apiRequest(
        `/api/v1/Tenants/${tenantToDelete.id}`,
        'delete'
      ));
      
      toast({
        title: 'Success',
        description: 'Tenant deleted successfully',
      });
      
      // Refresh tenants
      fetchTenants();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to delete tenant',
        variant: 'destructive',
      });
    } finally {
      setShowDeleteConfirm(false);
      setTenantToDelete(null);
    }
  };

  const handleFormSubmit = async (formData: any) => {
    // If editing, update existing tenant
    if (editingTenant) {
      try {
        await dispatch(apiRequest(
          `/api/v1/Tenants/${editingTenant.id}`,
          'put',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'Tenant updated successfully',
        });
        
        // Refresh tenants
        fetchTenants();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to update tenant',
          variant: 'destructive',
        });
        return false;
      }
    } 
    // Otherwise, create new tenant
    else {
      try {
        await dispatch(apiRequest(
          '/api/v1/Tenants',
          'post',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'Tenant created successfully',
        });
        
        // Refresh tenants
        fetchTenants();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to create tenant',
          variant: 'destructive',
        });
        return false;
      }
    }
    
    setShowForm(false);
    return true;
  };

  const handleConfigure = (tenant: Tenant) => {
    setConfiguringTenantId(tenant.id);
    setShowConfig(true);
  };

  const handleSecuritySettings = (tenant: Tenant) => {
    setSecuritySettingsTenantId(tenant.id);
    setShowSecuritySettings(true);
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-xl font-bold">Tenants</h2>
          <p className="text-muted-foreground">
            Manage organization tenants and their settings
          </p>
        </div>
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={fetchTenants}
            disabled={isLoading}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button 
            size="sm" 
            onClick={handleAddNew}
          >
            <Plus className="h-4 w-4 mr-2" />
            Add Tenant
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>
      ) : tenants.length === 0 ? (
        <div className="text-center py-8 border rounded-md bg-background">
          <p className="text-muted-foreground mb-4">No tenants found</p>
          <Button onClick={handleAddNew} variant="outline">
            <Plus className="h-4 w-4 mr-2" />
            Add your first tenant
          </Button>
        </div>
      ) : (
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Users</TableHead>
                <TableHead>Data Sources</TableHead>
                <TableHead>Created</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {tenants.map((tenant: Tenant) => (
                <TableRow key={tenant.id}>
                  <TableCell className="font-medium">{tenant.name}</TableCell>
                  <TableCell>{tenant.description}</TableCell>
                  <TableCell>
                    {tenant.isActive 
                      ? <Badge variant="success">Active</Badge>
                      : <Badge variant="secondary">Inactive</Badge>
                    }
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <span className="ml-2">{tenant.userCount || 0}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Database className="h-4 w-4 mr-1" />
                      <span>{tenant.dataSourceCount || 0}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Calendar className="h-4 w-4 mr-1" />
                      <span>{new Date(tenant.createdAt).toLocaleDateString()}</span>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleSecuritySettings(tenant)}
                        title="Security settings"
                      >
                        <Shield className="h-4 w-4" />
                        <span className="sr-only">Security</span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleConfigure(tenant)}
                        title="Configure tenant"
                      >
                        <Settings className="h-4 w-4" />
                        <span className="sr-only">Configure</span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleEdit(tenant)}
                        title="Edit tenant"
                      >
                        <Edit className="h-4 w-4" />
                        <span className="sr-only">Edit</span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleDelete(tenant)}
                        title="Delete tenant"
                      >
                        <Trash2 className="h-4 w-4" />
                        <span className="sr-only">Delete</span>
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Tenant Form Dialog */}
      {showForm && (
        <TenantForm
          tenant={editingTenant}
          onSubmit={handleFormSubmit}
          onCancel={() => setShowForm(false)}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {showDeleteConfirm && tenantToDelete && (
        <DeleteConfirmation
          title="Delete Tenant"
          message={`Are you sure you want to delete "${tenantToDelete.name}"? This action cannot be undone and will remove all associated data.`}
          onConfirm={confirmDelete}
          onCancel={() => {
            setShowDeleteConfirm(false);
            setTenantToDelete(null);
          }}
        />
      )}

      {/* Tenant Configuration Dialog */}
      {showConfig && (
        <TenantConfig
          tenantId={configuringTenantId}
          onClose={() => {
            setShowConfig(false);
            setConfiguringTenantId('');
            // Refresh tenant list to show any changes
            fetchTenants();
          }}
        />
      )}

      {/* Tenant Security Settings Dialog */}
      {showSecuritySettings && (
        <TenantSecuritySettings
          tenantId={securitySettingsTenantId}
          onClose={() => {
            setShowSecuritySettings(false);
            setSecuritySettingsTenantId('');
            // Refresh tenant list to show any changes
            fetchTenants();
          }}
        />
      )}
    </div>
  );
};

export default TenantList; 