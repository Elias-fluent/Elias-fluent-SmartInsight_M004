import React, { useState, useEffect } from 'react';
import { Button } from '../ui/button';
import { 
  Dialog, 
  DialogContent, 
  DialogHeader, 
  DialogTitle, 
  DialogFooter 
} from '../ui/dialog';
import { 
  Tabs, 
  TabsList, 
  TabsTrigger, 
  TabsContent 
} from '../ui/tabs';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '../ui/table';
import { useToast } from '../ui/use-toast';
import { 
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from '../ui/select';
import { Checkbox } from '../ui';
import { FormLabel } from '../ui/form';
import { Shield, Users, User, UserCheck, RefreshCw } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';

interface RoleAssignmentProps {
  onClose: () => void;
}

// For improved type safety
interface UserWithRoles {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  tenantId: string;
  tenantName?: string;
}

interface Tenant {
  id: string;
  name: string;
}

const RoleAssignment: React.FC<RoleAssignmentProps> = ({ onClose }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('byUser');
  const [selectedTenantId, setSelectedTenantId] = useState<string>('');
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [availableRoles, setAvailableRoles] = useState<string[]>(['Admin', 'User', 'Analyst', 'ReadOnly']);
  const [userRoles, setUserRoles] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  // Get data from Redux store
  const users = useSelector((state: RootState) => state.data.users) || [];
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];

  // Filter users by the selected tenant
  const filteredUsers = selectedTenantId 
    ? users.filter((user: UserWithRoles) => user.tenantId === selectedTenantId)
    : users;

  // Load available roles
  useEffect(() => {
    // Remove the fetchUserRoles call
    
    // Update the RoleAssignment with proper string type values
    setAvailableRoles([
      'Administrator',
      'User',
      'Analyst',
      'Viewer'
    ]);
  }, [selectedUserId, selectedTenantId]);

  // Load users
  useEffect(() => {
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

    fetchUsers();
  }, [dispatch, toast]);

  // Handle user selection
  const handleUserChange = (userId: string) => {
    setSelectedUserId(userId);
    const user = users.find((u: UserWithRoles) => u.id === userId);
    if (user) {
      setUserRoles(user.roles || []);
    } else {
      setUserRoles([]);
    }
  };

  // Handle role toggle
  const handleRoleToggle = (role: string, checked: boolean) => {
    if (checked) {
      setUserRoles(prev => [...prev, role]);
    } else {
      setUserRoles(prev => prev.filter(r => r !== role));
    }
  };

  // Save role changes
  const handleSaveRoles = async () => {
    if (!selectedUserId) {
      toast({
        title: 'Error',
        description: 'Please select a user',
        variant: 'destructive',
      });
      return;
    }

    setIsSaving(true);
    try {
      // Update user roles
      await dispatch(apiRequest(
        `/api/v1/Users/${selectedUserId}`,
        'put',
        { roles: userRoles }
      ));
      
      toast({
        title: 'Success',
        description: 'User roles updated successfully',
      });
      
      // Refresh users to reflect changes
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
        description: 'Failed to update user roles',
        variant: 'destructive',
      });
    } finally {
      setIsSaving(false);
    }
  };

  // Refresh users
  const handleRefresh = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/Users',
        'get',
        undefined,
        'FETCH_USERS_SUCCESS',
        'FETCH_USERS_FAILURE'
      ));
      
      toast({
        title: 'Success',
        description: 'User data refreshed',
      });
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to refresh users',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading && users.length === 0) {
    return (
      <Dialog open={true} onOpenChange={() => onClose()}>
        <DialogContent className="sm:max-w-[800px]">
          <div className="flex justify-center items-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
          </div>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <Dialog open={true} onOpenChange={() => onClose()}>
      <DialogContent className="sm:max-w-[800px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            <span>Role Assignment</span>
          </DialogTitle>
        </DialogHeader>

        <Tabs defaultValue="byUser" value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="mb-4 grid grid-cols-2">
            <TabsTrigger value="byUser" className="flex items-center gap-2">
              <User className="h-4 w-4" />
              <span>Assign by User</span>
            </TabsTrigger>
            <TabsTrigger value="byTenant" className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              <span>View by Tenant</span>
            </TabsTrigger>
          </TabsList>

          {/* Assign Roles by User Tab */}
          <TabsContent value="byUser" className="space-y-4">
            <div className="flex justify-between">
              <div className="space-y-2 flex-1 max-w-sm">
                <FormLabel htmlFor="user-select">Select User</FormLabel>
                <Select 
                  value={selectedUserId} 
                  onValueChange={handleUserChange}
                >
                  <SelectTrigger id="user-select">
                    <SelectValue placeholder="Select a user" />
                  </SelectTrigger>
                  <SelectContent>
                    {users.map((user: UserWithRoles) => (
                      <SelectItem key={user.id} value={user.id}>
                        {`${user.firstName} ${user.lastName} (${user.email})`}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleRefresh}
                disabled={isLoading}
                className="h-9 self-end"
              >
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>

            {selectedUserId && (
              <>
                <div className="border p-4 rounded-md space-y-3">
                  <h3 className="font-medium flex items-center gap-2">
                    <UserCheck className="h-4 w-4" />
                    <span>Assign Roles</span>
                  </h3>

                  <div className="grid grid-cols-2 gap-4 mt-2">
                    {availableRoles.map((role) => (
                      <div key={role} className="flex items-center space-x-2">
                        <Checkbox
                          id={`role-${role}`}
                          checked={userRoles.includes(role)}
                          onCheckedChange={(checked) => 
                            handleRoleToggle(role, Boolean(checked))
                          }
                        />
                        <FormLabel htmlFor={`role-${role}`} className="cursor-pointer font-normal">
                          {role}
                        </FormLabel>
                      </div>
                    ))}
                  </div>

                  {userRoles.length === 0 && (
                    <p className="text-sm text-amber-600">At least one role must be assigned to a user.</p>
                  )}
                </div>

                <div className="flex justify-end">
                  <Button 
                    onClick={handleSaveRoles}
                    disabled={isSaving || userRoles.length === 0}
                  >
                    {isSaving ? 'Saving...' : 'Save Role Assignments'}
                  </Button>
                </div>
              </>
            )}
          </TabsContent>

          {/* View by Tenant Tab */}
          <TabsContent value="byTenant" className="space-y-4">
            <div className="flex justify-between">
              <div className="space-y-2 flex-1 max-w-sm">
                <FormLabel htmlFor="tenant-select">Select Tenant</FormLabel>
                <Select 
                  value={selectedTenantId} 
                  onValueChange={setSelectedTenantId}
                >
                  <SelectTrigger id="tenant-select">
                    <SelectValue placeholder="Select a tenant" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="">All Tenants</SelectItem>
                    {tenants.map((tenant: Tenant) => (
                      <SelectItem key={tenant.id} value={tenant.id}>
                        {tenant.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleRefresh}
                disabled={isLoading}
                className="h-9 self-end"
              >
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>

            <div className="border rounded-md">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Tenant</TableHead>
                    <TableHead>Roles</TableHead>
                    <TableHead>Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredUsers.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center py-4 text-muted-foreground">
                        No users found
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredUsers.map((user: UserWithRoles) => (
                      <TableRow key={user.id}>
                        <TableCell className="font-medium">{`${user.firstName} ${user.lastName}`}</TableCell>
                        <TableCell>{user.email}</TableCell>
                        <TableCell>{user.tenantName || 'Unknown'}</TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            {user.roles && user.roles.length > 0 ? (
                              user.roles.map((role) => (
                                <Badge key={role} variant="outline">{role}</Badge>
                              ))
                            ) : (
                              <span className="text-muted-foreground text-sm">No roles</span>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          {user.isActive 
                            ? <Badge variant="success">Active</Badge>
                            : <Badge variant="secondary">Inactive</Badge>
                          }
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </TabsContent>
        </Tabs>

        <DialogFooter>
          <Button 
            type="button" 
            variant="outline" 
            onClick={onClose}
            disabled={isSaving}
          >
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default RoleAssignment; 