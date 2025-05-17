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
import { Plus, Edit, Trash2, RefreshCw, Shield, ShieldOff, UserCog } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';
import UserForm from './UserForm.tsx';
import DeleteConfirmation from './DeleteConfirmation.tsx';
import RoleAssignment from './RoleAssignment';

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

  // Get users from Redux store (will need to be added to data slice)
  const users = useSelector((state: RootState) => state.data.users) || [];

  // Fetch users on component mount
  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/Users',
        'get',
        undefined,
        'FETCH_USERS_SUCCESS',
        'FETCH_USERS_FAILURE'
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

  const toggleUserStatus = async (user: User) => {
    try {
      await dispatch(apiRequest(
        `/api/v1/Users/${user.id}/status`,
        'put',
        { isActive: !user.isActive }
      ));
      
      toast({
        title: 'Success',
        description: `User ${user.isActive ? 'deactivated' : 'activated'} successfully`,
      });
      
      // Refresh users
      fetchUsers();
    } catch (error) {
      toast({
        title: 'Error',
        description: `Failed to ${user.isActive ? 'deactivate' : 'activate'} user`,
        variant: 'destructive',
      });
    }
  };

  const confirmDelete = async () => {
    if (!userToDelete) return;

    try {
      await dispatch(apiRequest(
        `/api/v1/Users/${userToDelete.id}`,
        'delete'
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
      setShowDeleteConfirm(false);
      setUserToDelete(null);
    }
  };

  const handleFormSubmit = async (formData: any) => {
    // If editing, update existing user
    if (editingUser) {
      try {
        await dispatch(apiRequest(
          `/api/v1/Users/${editingUser.id}`,
          'put',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'User updated successfully',
        });
        
        // Refresh users
        fetchUsers();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to update user',
          variant: 'destructive',
        });
        return false;
      }
    } 
    // Otherwise, create new user
    else {
      try {
        await dispatch(apiRequest(
          '/api/v1/Users',
          'post',
          formData
        ));
        
        toast({
          title: 'Success',
          description: 'User created successfully',
        });
        
        // Refresh users
        fetchUsers();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to create user',
          variant: 'destructive',
        });
        return false;
      }
    }
    
    setShowForm(false);
    return true;
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
            <RefreshCw className="h-4 w-4 mr-2" />
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

      {isLoading ? (
        <div className="flex justify-center items-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>
      ) : users.length === 0 ? (
        <div className="text-center py-8 border rounded-md bg-background">
          <p className="text-muted-foreground mb-4">No users found</p>
          <Button onClick={handleAddNew} variant="outline">
            <Plus className="h-4 w-4 mr-2" />
            Add your first user
          </Button>
        </div>
      ) : (
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Tenant</TableHead>
                <TableHead>Roles</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last Login</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {users.map((user: User) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">{`${user.firstName} ${user.lastName}`}</TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>{user.tenantName || 'Unknown'}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {user.roles.map((role) => (
                        <Badge key={role} variant="outline">{role}</Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell>
                    {user.isActive 
                      ? <Badge variant="success">Active</Badge>
                      : <Badge variant="secondary">Inactive</Badge>
                    }
                  </TableCell>
                  <TableCell>
                    {user.lastLogin 
                      ? new Date(user.lastLogin).toLocaleString() 
                      : 'Never'}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleEdit(user)}
                        title="Edit user"
                      >
                        <Edit className="h-4 w-4" />
                        <span className="sr-only">Edit</span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => toggleUserStatus(user)}
                        title={user.isActive ? "Deactivate user" : "Activate user"}
                      >
                        {user.isActive 
                          ? <ShieldOff className="h-4 w-4" />
                          : <Shield className="h-4 w-4" />
                        }
                        <span className="sr-only">
                          {user.isActive ? 'Deactivate' : 'Activate'}
                        </span>
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleDelete(user)}
                        title="Delete user"
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

      {/* User Form Dialog */}
      {showForm && (
        <UserForm
          user={editingUser}
          onSubmit={handleFormSubmit}
          onCancel={() => setShowForm(false)}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {showDeleteConfirm && userToDelete && (
        <DeleteConfirmation
          title="Delete User"
          message={`Are you sure you want to delete "${userToDelete.firstName} ${userToDelete.lastName}"? This action cannot be undone.`}
          onConfirm={confirmDelete}
          onCancel={() => {
            setShowDeleteConfirm(false);
            setUserToDelete(null);
          }}
        />
      )}

      {/* Role Assignment Dialog */}
      {showRoleAssignment && (
        <RoleAssignment
          onClose={() => setShowRoleAssignment(false)}
        />
      )}
    </div>
  );
};

export default UserList; 