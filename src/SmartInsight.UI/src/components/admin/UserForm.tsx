import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useToast } from '../ui/use-toast';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { 
  Input,
  Button,
  Switch,
  Form,
  FormDescription,
  FormLabel,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Checkbox
} from '../ui';
import z from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

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

// Form validation schema
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
  isActive: z.boolean().default(true),
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
  const [availableRoles, setAvailableRoles] = useState<string[]>(['Admin', 'User', 'Analyst', 'ReadOnly']);
  
  // Get tenants from Redux store (will need to be added to data slice)
  const tenants = useSelector((state: RootState) => state.data.tenants) || [];

  // Form setup
  const form = useForm<UserFormValues>({
    resolver: zodResolver(userFormSchema) as any,
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

  // Fetch roles if needed (if we want to dynamically load them from API)
  useEffect(() => {
    const fetchRoles = async () => {
      try {
        const response = await dispatch(apiRequest(
          '/api/v1/Roles',
          'get'
        ));
        if (response?.payload) {
          setAvailableRoles(response.payload);
        }
      } catch (error) {
        // Use default roles on error
        console.error('Failed to fetch roles, using defaults', error);
      }
    };

    // Uncomment this when the API is ready
    // fetchRoles();
  }, []);

  // Handle form submission
  const handleFormSubmit = async (data: UserFormValues) => {
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
            <div className="space-y-2">
              <FormLabel htmlFor="firstName">First Name</FormLabel>
              <Input 
                id="firstName"
                placeholder="First name" 
                {...form.register('firstName')} 
              />
              {form.formState.errors.firstName && (
                <p className="text-sm text-red-500">{form.formState.errors.firstName.message}</p>
              )}
            </div>
            
            <div className="space-y-2">
              <FormLabel htmlFor="lastName">Last Name</FormLabel>
              <Input 
                id="lastName" 
                placeholder="Last name" 
                {...form.register('lastName')} 
              />
              {form.formState.errors.lastName && (
                <p className="text-sm text-red-500">{form.formState.errors.lastName.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <FormLabel htmlFor="email">Email</FormLabel>
            <Input 
              id="email"
              type="email" 
              placeholder="user@example.com" 
              {...form.register('email')} 
            />
            {form.formState.errors.email && (
              <p className="text-sm text-red-500">{form.formState.errors.email.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <FormLabel htmlFor="tenantId">Tenant</FormLabel>
            <select
              id="tenantId"
              className="w-full p-2 border rounded"
              {...form.register('tenantId')}
            >
              <option value="">Select tenant</option>
              {tenants.map((tenant: Tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}
                </option>
              ))}
            </select>
            {form.formState.errors.tenantId && (
              <p className="text-sm text-red-500">{form.formState.errors.tenantId.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <FormLabel>Roles</FormLabel>
            <div className="grid grid-cols-2 gap-2">
              {availableRoles.map((role) => (
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
              ))}
            </div>
            {form.formState.errors.roles && (
              <p className="text-sm text-red-500">{form.formState.errors.roles.message}</p>
            )}
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox
              id="isActive"
              checked={form.watch('isActive')}
              onCheckedChange={(checked) => 
                form.setValue('isActive', checked as boolean)
              }
            />
            <FormLabel htmlFor="isActive" className="cursor-pointer">
              Active
            </FormLabel>
          </div>

          {/* Password fields - only required for new users */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <FormLabel htmlFor="password">{user ? 'New Password (optional)' : 'Password'}</FormLabel>
              <Input 
                id="password"
                type="password" 
                placeholder={user ? "Leave blank to keep current" : "Password"} 
                {...form.register('password')} 
              />
              {form.formState.errors.password && (
                <p className="text-sm text-red-500">{form.formState.errors.password.message}</p>
              )}
            </div>
            
            <div className="space-y-2">
              <FormLabel htmlFor="confirmPassword">Confirm Password</FormLabel>
              <Input 
                id="confirmPassword"
                type="password" 
                placeholder="Confirm password" 
                {...form.register('confirmPassword')} 
              />
              {form.formState.errors.confirmPassword && (
                <p className="text-sm text-red-500">{form.formState.errors.confirmPassword.message}</p>
              )}
            </div>
          </div>

          <DialogFooter>
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
              {isSubmitting ? 'Saving...' : user ? 'Update User' : 'Create User'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default UserForm; 