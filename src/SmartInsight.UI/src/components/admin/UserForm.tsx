import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useToast } from '../ui/use-toast';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { 
  Input,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Checkbox,
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
  Form
} from '../ui';
import { Loader2 } from 'lucide-react';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';

interface Tenant {
  id: string;
  name: string;
}

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  tenantId: string;
}

interface UserFormProps {
  user: User | null;
  onSubmit: (data: any) => Promise<boolean>;
  onCancel: () => void;
}

// Form validation schema with proper typing
const userFormSchema = z.object({
  email: z.string().email({ message: 'Please enter a valid email address' }),
  firstName: z.string().min(1, { message: 'First name is required' }),
  lastName: z.string().min(1, { message: 'Last name is required' }),
  password: z.string()
    .min(8, { message: 'Password must be at least 8 characters' })
    .regex(/[0-9]/, { message: 'Password must contain at least one number' })
    .optional()
    .or(z.literal('')),
  confirmPassword: z.string().optional().or(z.literal('')),
  roles: z.array(z.string()).min(1, { message: 'At least one role is required' }),
  isActive: z.boolean(),
  tenantId: z.string().min(1, { message: 'Tenant is required' }),
})
.refine(data => !data.password || data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

// Define the form data type based on the schema
type UserFormValues = z.infer<typeof userFormSchema>;

const UserForm: React.FC<UserFormProps> = ({ user, onSubmit, onCancel }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [isLoadingRoles, setIsLoadingRoles] = useState(false);
  
  // Get tenants from Redux store
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];

  // Form setup with simplified typing
  const form = useForm({
    resolver: zodResolver(userFormSchema),
    defaultValues: {
      email: user?.email || '',
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      password: '',
      confirmPassword: '',
      roles: user?.roles || [],
      isActive: user?.isActive !== undefined ? user.isActive : true,
      tenantId: user?.tenantId || '',
    },
  });

  // Fetch roles on component mount
  useEffect(() => {
    fetchRoles();
    // If tenants array is empty, fetch tenants
    if (tenants.length === 0) {
      fetchTenants();
    }
  }, []);

  const fetchRoles = async () => {
    setIsLoadingRoles(true);
    try {
      const response = await dispatch(apiRequest(
        '/api/v1/Roles',
        'get',
        undefined,
        DATA_ACTIONS.FETCH_ROLES_SUCCESS,
        DATA_ACTIONS.FETCH_ROLES_FAILURE
      ));
      
      if (response?.success && response.payload) {
        setAvailableRoles(response.payload);
      } else {
        // Fallback to default roles if API fails
        setAvailableRoles(['Administrator', 'User', 'Analyst', 'ReadOnly']);
      }
    } catch (error) {
      // Use default roles on error
      setAvailableRoles(['Administrator', 'User', 'Analyst', 'ReadOnly']);
      console.error('Failed to fetch roles, using defaults', error);
    } finally {
      setIsLoadingRoles(false);
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

  // Handle form submission
  const handleFormSubmit = async (data: any) => {
    setIsSubmitting(true);
    
    try {
      // Remove confirmPassword as it's not needed in the API
      const formData = { ...data };
      delete (formData as any).confirmPassword;
      
      // If password is empty, remove it (for user updates)
      if (!formData.password) {
        delete formData.password;
      }

      const success = await onSubmit(formData);
      if (success) {
        form.reset();
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to submit user data',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={true} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {user ? 'Edit User' : 'Add New User'}
          </DialogTitle>
        </DialogHeader>
        
        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="firstName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input placeholder="First name" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            <FormField
              control={form.control}
              name="lastName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Last Name</FormLabel>
                  <FormControl>
                    <Input placeholder="Last name" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="user@example.com" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="tenantId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Tenant</FormLabel>
                <Select 
                  onValueChange={field.onChange} 
                  defaultValue={field.value}
                >
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select tenant" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {tenants.map((tenant: Tenant) => (
                      <SelectItem key={tenant.id} value={tenant.id}>
                        {tenant.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />

          <div className="space-y-2">
            <FormLabel>Roles</FormLabel>
            <div className="grid grid-cols-2 gap-2 border rounded-md p-3">
              {isLoadingRoles ? (
                <div className="col-span-2 flex justify-center py-2">
                  <Loader2 className="h-5 w-5 animate-spin" />
                </div>
              ) : (
                availableRoles.map((role) => (
                  <div key={role} className="flex items-center space-x-2">
                    <Checkbox
                      id={`role-${role}`}
                      checked={form.watch('roles')?.includes(role)}
                      onCheckedChange={(checked) => {
                        const currentRoles = form.watch('roles') || [];
                        const updatedRoles = checked
                          ? [...currentRoles, role]
                          : currentRoles.filter((r) => r !== role);
                        form.setValue('roles', updatedRoles, { shouldValidate: true });
                      }}
                    />
                    <FormLabel htmlFor={`role-${role}`} className="cursor-pointer font-normal">
                      {role}
                    </FormLabel>
                  </div>
                ))
              )}
            </div>
            {form.formState.errors.roles && (
              <p className="text-sm text-red-500">{form.formState.errors.roles.message}</p>
            )}
          </div>

          <FormField
            control={form.control}
            name="isActive"
            render={({ field }) => (
              <FormItem className="flex flex-row items-center space-x-3 space-y-0 rounded-md border p-3">
                <FormControl>
                  <Checkbox
                    checked={field.value}
                    onCheckedChange={field.onChange}
                  />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel>Active</FormLabel>
                  <FormDescription>
                    Inactive users cannot log in to the system
                  </FormDescription>
                </div>
              </FormItem>
            )}
          />

          {/* Only show password fields when creating a new user or explicitly editing */}
          <div className={user ? 'border-t pt-4' : ''}>
            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>
                    {user ? 'New Password (leave blank to keep current)' : 'Password'}
                  </FormLabel>
                  <FormControl>
                    <Input 
                      type="password" 
                      placeholder={user ? "••••••••" : "Enter password"} 
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="confirmPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Confirm Password</FormLabel>
                  <FormControl>
                    <Input 
                      type="password" 
                      placeholder="Confirm password"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <DialogFooter className="mt-6">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button 
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              {user ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default UserForm; 