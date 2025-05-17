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
import { Checkbox } from '../ui/checkbox';
import { FormLabel } from '../ui/form';
import { Shield, Users, User, UserCheck, RefreshCw } from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { Badge } from '../ui/badge';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';

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
  const [availableRoles, setAvailableRoles] = useState<string[]>(['Administrator', 'User', 'Analyst', 'Viewer']);
  const [userRoles, setUserRoles] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  // Get data from Redux store
  const users = useSelector((state: RootState) => state.data.users) || [];
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];

  // Filter users by the selected tenant
  const filteredUsers = selectedTenantId 
    ? users.filter((user: UserWithRoles) => user.tenantId === selectedTenantId)
    : users;

  // Load users if not already loaded
  useEffect(() => {
    if (users.length === 0) {
      fetchUsers();
    }
  }, []);

  // Load tenants if not already loaded
  useEffect(() => {
    if (tenants.length === 0) {
      fetchTenants();
    }
  }, []);

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

  const fetchTenants = async () => {
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
    }
  };

  const handleRefresh = () => {
    fetchUsers();
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
        `/api/v1/Users/${selectedUserId}/roles`,
        'put',
        { roles: userRoles },
        DATA_ACTIONS.UPDATE_USER_ROLES_SUCCESS,
        DATA_ACTIONS.UPDATE_USER_ROLES_FAILURE
      ));
      
      toast({
        title: 'Success',
        description: 'User roles updated successfully',
      });
      
      // Refresh users to reflect changes
      fetchUsers();
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

          <TabsContent value="byUser">
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
                <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
            </div>

            {selectedUserId && (
              <>
                <div className="border p-4 rounded-md space-y-3 mt-4">
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

                <div className="flex justify-end mt-4">
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

          <TabsContent value="byTenant">
            <div className="space-y-4">
              <div className="space-y-2 max-w-sm">
                <FormLabel htmlFor="tenant-select">Select Tenant</FormLabel>
                <Select 
                  value={selectedTenantId} 
                  onValueChange={setSelectedTenantId}
                >
                  <SelectTrigger id="tenant-select">
                    <SelectValue placeholder="Select a tenant" />
                  </SelectTrigger>
                  <SelectContent>
                    {tenants.map((tenant: Tenant) => (
                      <SelectItem key={tenant.id} value={tenant.id}>
                        {tenant.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {selectedTenantId && (
                <div className="border rounded-md overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>User</TableHead>
                        <TableHead>Email</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Roles</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredUsers.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={4} className="text-center py-6 text-muted-foreground">
                            No users found for this tenant
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredUsers.map((user: UserWithRoles) => (
                          <TableRow key={user.id}>
                            <TableCell className="font-medium">{`${user.firstName} ${user.lastName}`}</TableCell>
                            <TableCell>{user.email}</TableCell>
                            <TableCell>
                              <Badge variant={user.isActive ? 'success' : 'secondary'}>
                                {user.isActive ? 'Active' : 'Inactive'}
                              </Badge>
                            </TableCell>
                            <TableCell>
                              <div className="flex flex-wrap gap-1">
                                {user.roles && user.roles.map((role, index) => (
                                  <Badge key={index} variant="outline">{role}</Badge>
                                ))}
                              </div>
                            </TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </div>
              )}
            </div>
          </TabsContent>
        </Tabs>

        <DialogFooter className="mt-6">
          <Button onClick={onClose}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default RoleAssignment; 