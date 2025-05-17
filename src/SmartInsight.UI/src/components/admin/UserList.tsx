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
import { Plus, Edit, Trash2, RefreshCw, Shield, ShieldOff, UserCog, Search } from 'lucide-react';
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
import UserForm from './UserForm';
import DeleteConfirmation from './DeleteConfirmation';
import RoleAssignment from './RoleAssignment';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';
import { ConfirmationDialog } from '../ui/confirmation-dialog';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  tenantId: string;
  tenantName?: string;
  lastLogin?: string;
}

const UserList: React.FC = () => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [showRoleAssignment, setShowRoleAssignment] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  
  // Pagination state
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);

  // Status toggle confirmation
  const [showStatusConfirm, setShowStatusConfirm] = useState(false);
  const [userToToggle, setUserToToggle] = useState<User | null>(null);
  
  // Get users from Redux store
  const users = useSelector((state: RootState) => state.data.users) || [];
  const error = useSelector((state: RootState) => state.data.error);

  // Filter users by search query
  const filteredUsers = users.filter(user => 
    user.firstName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    user.lastName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    user.email?.toLowerCase().includes(searchQuery.toLowerCase())
  );
  
  // Calculate total pages
  useEffect(() => {
    setTotalPages(Math.max(1, Math.ceil(filteredUsers.length / pageSize)));
    // Reset to first page when filter changes
    if (currentPage > 1 && filteredUsers.length <= (currentPage - 1) * pageSize) {
      setCurrentPage(1);
    }
  }, [filteredUsers.length, pageSize, currentPage]);
  
  // Get paginated users
  const paginatedUsers = filteredUsers.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  );
  
  // Fetch users on mount
  useEffect(() => {
    fetchUsers();
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

  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/Users',
        'get',
        undefined,
        DATA_ACTIONS.FETCH_USERS_SUCCESS,
        DATA_ACTIONS.FETCH_USERS_FAILURE
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch users',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddNew = () => {
    setEditingUser(null);
    setShowForm(true);
  };

  const handleEdit = (user: User) => {
    setEditingUser(user);
    setShowForm(true);
  };

  const handleDelete = (user: User) => {
    setUserToDelete(user);
    setShowDeleteConfirm(true);
  };
  
  const handleManageRoles = () => {
    setShowRoleAssignment(true);
  };

  const handleToggleUserStatus = (user: User) => {
    setUserToToggle(user);
    setShowStatusConfirm(true);
  };

  const toggleUserStatus = async () => {
    if (!userToToggle) return;
    
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        `/api/v1/Users/${userToToggle.id}/status`,
        'put',
        { isActive: !userToToggle.isActive },
        DATA_ACTIONS.UPDATE_USER_STATUS_SUCCESS,
        DATA_ACTIONS.UPDATE_USER_STATUS_FAILURE
      ));
      
      toast({
        title: 'Success',
        description: `User ${userToToggle.isActive ? 'deactivated' : 'activated'} successfully`,
      });
      
      // Refresh users
      fetchUsers();
    } catch (error) {
      toast({
        title: 'Error',
        description: `Failed to ${userToToggle.isActive ? 'deactivate' : 'activate'} user`,
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
      setShowStatusConfirm(false);
      setUserToToggle(null);
    }
  };

  const confirmDelete = async () => {
    if (!userToDelete) return;

    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        `/api/v1/Users/${userToDelete.id}`,
        'delete',
        undefined,
        DATA_ACTIONS.DELETE_USER_SUCCESS,
        DATA_ACTIONS.DELETE_USER_FAILURE
      ));
      
      toast({
        title: 'Success',
        description: 'User deleted successfully',
      });
      
      // Refresh users
      fetchUsers();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to delete user',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
      setShowDeleteConfirm(false);
      setUserToDelete(null);
    }
  };

  const handleFormSubmit = async (formData: any) => {
    setIsLoading(true);
    
    // If editing, update existing user
    if (editingUser) {
      try {
        await dispatch(apiRequest(
          `/api/v1/Users/${editingUser.id}`,
          'put',
          formData,
          DATA_ACTIONS.UPDATE_USER_SUCCESS,
          DATA_ACTIONS.UPDATE_USER_FAILURE
        ));
        
        toast({
          title: 'Success',
          description: 'User updated successfully',
        });
        
        // Refresh users
        fetchUsers();
        setShowForm(false);
        return true;
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to update user',
          variant: 'destructive',
        });
        setIsLoading(false);
        return false;
      }
    } 
    // Otherwise, create new user
    else {
      try {
        await dispatch(apiRequest(
          '/api/v1/Users',
          'post',
          formData,
          DATA_ACTIONS.CREATE_USER_SUCCESS,
          DATA_ACTIONS.CREATE_USER_FAILURE
        ));
        
        toast({
          title: 'Success',
          description: 'User created successfully',
        });
        
        // Refresh users
        fetchUsers();
        setShowForm(false);
        return true;
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to create user',
          variant: 'destructive',
        });
        setIsLoading(false);
        return false;
      }
    }
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingUser(null);
  };

  const handleCloseRoleAssignment = () => {
    setShowRoleAssignment(false);
    fetchUsers(); // Refresh users after role assignment
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

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-xl font-bold">Users</h2>
          <p className="text-muted-foreground">
            Manage users and their permissions
          </p>
        </div>
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={fetchUsers}
            disabled={isLoading}
          >
            <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button 
            variant="outline"
            size="sm"
            onClick={handleManageRoles}
          >
            <UserCog className="h-4 w-4 mr-2" />
            Manage Roles
          </Button>
          <Button 
            size="sm"
            onClick={handleAddNew}
          >
            <Plus className="h-4 w-4 mr-2" />
            Add User
          </Button>
        </div>
      </div>

      <div className="flex justify-between mb-4">
        <div className="relative w-64">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search users..."
            className="pl-8"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Roles</TableHead>
              <TableHead>Tenant</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {paginatedUsers.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-6 text-muted-foreground">
                  {isLoading ? 'Loading users...' : 'No users found'}
                </TableCell>
              </TableRow>
            ) : (
              paginatedUsers.map((user: User) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">{`${user.firstName} ${user.lastName}`}</TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {user.roles && user.roles.map((role, index) => (
                        <Badge key={index} variant="outline">{role}</Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell>{user.tenantName || '-'}</TableCell>
                  <TableCell>
                    <Badge variant={user.isActive ? 'success' : 'secondary'}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleEdit(user)}
                        title="Edit user"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleToggleUserStatus(user)}
                        title={user.isActive ? "Deactivate user" : "Activate user"}
                      >
                        {user.isActive ? (
                          <ShieldOff className="h-4 w-4" />
                        ) : (
                          <Shield className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(user)}
                        title="Delete user"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {renderPagination()}

      {showForm && (
        <UserForm
          user={editingUser}
          onSubmit={handleFormSubmit}
          onCancel={handleCloseForm}
        />
      )}

      <ConfirmationDialog
        open={showDeleteConfirm}
        onOpenChange={setShowDeleteConfirm}
        title="Delete User"
        description={`Are you sure you want to delete the user "${userToDelete?.firstName} ${userToDelete?.lastName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        variant="destructive"
        isLoading={isLoading}
        onConfirm={confirmDelete}
      />

      <ConfirmationDialog
        open={showStatusConfirm}
        onOpenChange={setShowStatusConfirm}
        title={userToToggle?.isActive ? "Deactivate User" : "Activate User"}
        description={`Are you sure you want to ${userToToggle?.isActive ? 'deactivate' : 'activate'} the user "${userToToggle?.firstName} ${userToToggle?.lastName}"?${userToToggle?.isActive ? ' Deactivated users cannot log in to the system.' : ''}`}
        confirmLabel={userToToggle?.isActive ? "Deactivate" : "Activate"}
        variant={userToToggle?.isActive ? "destructive" : "default"}
        isLoading={isLoading}
        onConfirm={toggleUserStatus}
      />

      {showRoleAssignment && (
        <RoleAssignment onClose={handleCloseRoleAssignment} />
      )}
    </div>
  );
};

export default UserList; 