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
import { Plus, Edit, Trash2, RefreshCw, Database, Calendar, Settings, Shield, Search } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';
import { Input } from '../ui/input';
import { 
  Pagination, 
  PaginationContent, 
  PaginationItem, 
  PaginationLink, 
  PaginationNext, 
  PaginationPrevious 
} from '../ui/pagination';
import TenantForm from './TenantForm';
import TenantConfig from './TenantConfig';
import TenantSecuritySettings from './TenantSecuritySettings';
import DeleteConfirmation from './DeleteConfirmation';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';

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
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [tenantsPerPage] = useState(10);

  // Get tenants from Redux store
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];
  const error = useSelector((state: RootState) => state.data.error);

  // Filter tenants based on search term
  const filteredTenants = tenants.filter(tenant => 
    tenant.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    tenant.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Paginate tenants
  const indexOfLastTenant = currentPage * tenantsPerPage;
  const indexOfFirstTenant = indexOfLastTenant - tenantsPerPage;
  const currentTenants = filteredTenants.slice(indexOfFirstTenant, indexOfLastTenant);
  const totalPages = Math.ceil(filteredTenants.length / tenantsPerPage);

  // Fetch tenants on component mount
  useEffect(() => {
    fetchTenants();
  }, []);

  // Show error toast if there's an error
  useEffect(() => {
    if (error) {
      toast({
        title: 'Error',
        description: error,
        variant: 'destructive',
      });
      // Clear error
      dispatch({ type: DATA_ACTIONS.CLEAR_ERROR });
    }
  }, [error, dispatch, toast]);

  const fetchTenants = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/Tenants',
        'get',
        undefined,
        DATA_ACTIONS.FETCH_TENANTS_SUCCESS,
        DATA_ACTIONS.FETCH_TENANTS_FAILURE
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

    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        `/api/v1/Tenants/${tenantToDelete.id}`,
        'delete',
        undefined,
        DATA_ACTIONS.DELETE_TENANT_SUCCESS,
        DATA_ACTIONS.DELETE_TENANT_FAILURE
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
      setIsLoading(false);
      setShowDeleteConfirm(false);
      setTenantToDelete(null);
    }
  };

  const handleFormSubmit = async (formData: any) => {
    setIsLoading(true);
    
    // If editing, update existing tenant
    if (editingTenant) {
      try {
        await dispatch(apiRequest(
          `/api/v1/Tenants/${editingTenant.id}`,
          'put',
          formData,
          DATA_ACTIONS.UPDATE_TENANT_SUCCESS,
          DATA_ACTIONS.UPDATE_TENANT_FAILURE
        ));
        
        toast({
          title: 'Success',
          description: 'Tenant updated successfully',
        });
        
        // Refresh tenants
        fetchTenants();
        setShowForm(false);
        return true;
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to update tenant',
          variant: 'destructive',
        });
        setIsLoading(false);
        return false;
      }
    } 
    // Otherwise, create new tenant
    else {
      try {
        await dispatch(apiRequest(
          '/api/v1/Tenants',
          'post',
          formData,
          DATA_ACTIONS.CREATE_TENANT_SUCCESS,
          DATA_ACTIONS.CREATE_TENANT_FAILURE
        ));
        
        toast({
          title: 'Success',
          description: 'Tenant created successfully',
        });
        
        // Refresh tenants
        fetchTenants();
        setShowForm(false);
        return true;
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to create tenant',
          variant: 'destructive',
        });
        setIsLoading(false);
        return false;
      }
    }
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingTenant(null);
  };

  const handleConfigure = (tenant: Tenant) => {
    setConfiguringTenantId(tenant.id);
    setShowConfig(true);
  };

  const handleSecuritySettings = (tenant: Tenant) => {
    setSecuritySettingsTenantId(tenant.id);
    setShowSecuritySettings(true);
  };

  const handleCloseConfig = () => {
    setShowConfig(false);
    setConfiguringTenantId('');
  };

  const handleCloseSecuritySettings = () => {
    setShowSecuritySettings(false);
    setSecuritySettingsTenantId('');
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const renderPagination = () => {
    if (totalPages <= 1) return null;

    return (
      <Pagination className="mt-4">
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious 
              size="default"
              onClick={() => handlePageChange(Math.max(1, currentPage - 1))}
              className={currentPage === 1 ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
            />
          </PaginationItem>
          
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
            <PaginationItem key={page}>
              <PaginationLink
                size="default"
                onClick={() => handlePageChange(page)}
                isActive={page === currentPage}
                className="cursor-pointer"
              >
                {page}
              </PaginationLink>
            </PaginationItem>
          ))}
          
          <PaginationItem>
            <PaginationNext 
              size="default"
              onClick={() => handlePageChange(Math.min(totalPages, currentPage + 1))}
              className={currentPage === totalPages ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
            />
          </PaginationItem>
        </PaginationContent>
      </Pagination>
    );
  };

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleDateString();
    } catch (error) {
      return 'Invalid date';
    }
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
            <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
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

      <div className="mb-4">
        <div className="relative">
          <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
          <Input 
            placeholder="Search tenants by name or description..."
            className="pl-10"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setCurrentPage(1); // Reset to first page on search
            }}
          />
        </div>
      </div>

      {isLoading && tenants.length === 0 ? (
        <div className="flex justify-center items-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>
      ) : filteredTenants.length === 0 ? (
        <div className="text-center py-8 border rounded-md bg-background">
          <p className="text-muted-foreground mb-4">
            {searchTerm ? 'No tenants match your search' : 'No tenants found'}
          </p>
          {!searchTerm && (
            <Button onClick={handleAddNew} variant="outline">
              <Plus className="h-4 w-4 mr-2" />
              Add your first tenant
            </Button>
          )}
        </div>
      ) : (
        <>
          <div className="border rounded-md overflow-hidden">
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
                {currentTenants.map((tenant: Tenant) => (
                  <TableRow key={tenant.id}>
                    <TableCell className="font-medium">{tenant.name}</TableCell>
                    <TableCell>{tenant.description}</TableCell>
                    <TableCell>
                      {tenant.isActive 
                        ? <Badge variant="success">Active</Badge>
                        : <Badge variant="secondary">Inactive</Badge>
                      }
                    </TableCell>
                    <TableCell>{tenant.userCount || 0}</TableCell>
                    <TableCell>{tenant.dataSourceCount || 0}</TableCell>
                    <TableCell>{formatDate(tenant.createdAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button 
                          variant="ghost" 
                          size="icon"
                          onClick={() => handleSecuritySettings(tenant)}
                          title="Security Settings"
                        >
                          <Shield className="h-4 w-4" />
                        </Button>
                        <Button 
                          variant="ghost" 
                          size="icon"
                          onClick={() => handleConfigure(tenant)}
                          title="Configure Tenant"
                        >
                          <Settings className="h-4 w-4" />
                        </Button>
                        <Button 
                          variant="ghost" 
                          size="icon"
                          onClick={() => handleEdit(tenant)}
                          title="Edit Tenant"
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button 
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(tenant)}
                          title="Delete Tenant"
                        >
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          
          {renderPagination()}
        </>
      )}

      {/* Tenant form dialog */}
      {showForm && (
        <TenantForm 
          tenant={editingTenant} 
          onSubmit={handleFormSubmit} 
          onCancel={handleCloseForm}
        />
      )}

      {/* Delete confirmation dialog */}
      {showDeleteConfirm && tenantToDelete && (
        <DeleteConfirmation
          title="Delete Tenant"
          message={`Are you sure you want to delete ${tenantToDelete.name}? This action cannot be undone and will remove all associated data.`}
          onConfirm={confirmDelete}
          onCancel={() => {
            setShowDeleteConfirm(false);
            setTenantToDelete(null);
          }}
        />
      )}

      {/* Tenant configuration dialog */}
      {showConfig && configuringTenantId && (
        <TenantConfig
          tenantId={configuringTenantId}
          onClose={handleCloseConfig}
        />
      )}

      {/* Tenant security settings dialog */}
      {showSecuritySettings && securitySettingsTenantId && (
        <TenantSecuritySettings
          tenantId={securitySettingsTenantId}
          onClose={handleCloseSecuritySettings}
        />
      )}
    </div>
  );
};

export default TenantList; 