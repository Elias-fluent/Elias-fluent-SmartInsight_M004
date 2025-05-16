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
import { Plus, Edit, Trash2, RefreshCw } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';
import DataSourceForm from './DataSourceForm';
import DeleteConfirmation from './DeleteConfirmation';

const DataSourceList: React.FC = () => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingDataSource, setEditingDataSource] = useState<any>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [dataSourceToDelete, setDataSourceToDelete] = useState<any>(null);

  // Get data sources from Redux store
  const dataSources = useSelector((state: RootState) => state.data.dataSources) || [];

  // Fetch data sources on component mount
  useEffect(() => {
    fetchDataSources();
  }, []);

  const fetchDataSources = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/DataSource',
        'get',
        undefined,
        'FETCH_DATA_SOURCES_SUCCESS',
        'FETCH_DATA_SOURCES_FAILURE'
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch data sources',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddNew = () => {
    setEditingDataSource(null);
    setShowForm(true);
  };

  const handleEdit = (dataSource: any) => {
    setEditingDataSource(dataSource);
    setShowForm(true);
  };

  const handleDelete = (dataSource: any) => {
    setDataSourceToDelete(dataSource);
    setShowDeleteConfirm(true);
  };

  const confirmDelete = async () => {
    if (!dataSourceToDelete) return;

    try {
      await dispatch(apiRequest(
        `/api/v1/DataSource/${dataSourceToDelete.id}`,
        'delete'
      ));
      
      toast({
        title: 'Success',
        description: 'Data source deleted successfully',
      });
      
      // Refresh data sources
      fetchDataSources();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to delete data source',
        variant: 'destructive',
      });
    } finally {
      setShowDeleteConfirm(false);
      setDataSourceToDelete(null);
    }
  };

  const handleFormSubmit = async (formData: any) => {
    // If editing, update existing data source
    if (editingDataSource) {
      try {
        await dispatch(apiRequest(
          `/api/v1/DataSource/${editingDataSource.id}`,
          'put',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'Data source updated successfully',
        });
        
        // Refresh data sources
        fetchDataSources();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to update data source',
          variant: 'destructive',
        });
        return false;
      }
    } 
    // Otherwise, create new data source
    else {
      try {
        await dispatch(apiRequest(
          '/api/v1/DataSource',
          'post',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'Data source created successfully',
        });
        
        // Refresh data sources
        fetchDataSources();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to create data source',
          variant: 'destructive',
        });
        return false;
      }
    }
    
    setShowForm(false);
    return true;
  };

  // Helper to get badge color based on connection status
  const getConnectionStatusBadge = (status: string) => {
    switch (status) {
      case 'connected':
        return <Badge variant="success">Connected</Badge>;
      case 'disconnected':
        return <Badge variant="secondary">Disconnected</Badge>;
      case 'error':
        return <Badge variant="destructive">Error</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  // Format data source type for display
  const formatDataSourceType = (type: string) => {
    switch (type.toLowerCase()) {
      case 'postgresql': return 'PostgreSQL';
      case 'mysql': return 'MySQL';
      case 'mssql': return 'SQL Server';
      case 'file': return 'File System';
      case 'restapi': return 'REST API';
      case 'graphql': return 'GraphQL API';
      case 'confluence': return 'Confluence';
      case 'jira': return 'JIRA';
      case 'fileshare': return 'File Share';
      case 'git': return 'Git Repository';
      case 'svn': return 'SVN Repository';
      default: return type;
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-xl font-bold">Data Sources</h2>
          <p className="text-muted-foreground">
            Manage connections to your external data sources
          </p>
        </div>
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={fetchDataSources}
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
            Add New
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>
      ) : dataSources.length === 0 ? (
        <div className="text-center py-8 border rounded-md bg-background">
          <p className="text-muted-foreground mb-4">No data sources configured yet</p>
          <Button onClick={handleAddNew} variant="outline">
            <Plus className="h-4 w-4 mr-2" />
            Add your first data source
          </Button>
        </div>
      ) : (
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last Sync</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {dataSources.map((dataSource: any) => (
                <TableRow key={dataSource.id}>
                  <TableCell className="font-medium">{dataSource.name}</TableCell>
                  <TableCell>{formatDataSourceType(dataSource.type)}</TableCell>
                  <TableCell>{getConnectionStatusBadge(dataSource.connectionStatus)}</TableCell>
                  <TableCell>
                    {dataSource.lastSync 
                      ? new Date(dataSource.lastSync).toLocaleString() 
                      : 'Never'}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleEdit(dataSource)}
                      >
                        <Edit className="h-4 w-4" />
                        <span className="sr-only">Edit</span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleDelete(dataSource)}
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

      {/* Data Source Form Dialog */}
      {showForm && (
        <DataSourceForm
          dataSource={editingDataSource}
          onSubmit={handleFormSubmit}
          onCancel={() => setShowForm(false)}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {showDeleteConfirm && dataSourceToDelete && (
        <DeleteConfirmation
          title="Delete Data Source"
          message={`Are you sure you want to delete "${dataSourceToDelete.name}"? This action cannot be undone.`}
          onConfirm={confirmDelete}
          onCancel={() => {
            setShowDeleteConfirm(false);
            setDataSourceToDelete(null);
          }}
        />
      )}
    </div>
  );
};

export default DataSourceList; 