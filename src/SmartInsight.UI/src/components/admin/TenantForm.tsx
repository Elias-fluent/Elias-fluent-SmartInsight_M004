import React, { useState } from 'react';
import { useToast } from '../ui/use-toast';
import { 
  Input,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Checkbox,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
  Form
} from '../ui';
import { Textarea } from '../ui/textarea';
import { Loader2 } from 'lucide-react';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

interface Tenant {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  connectionString?: string;
  maxUsers?: number;
  maxConnections?: number;
}

interface TenantFormProps {
  tenant: Tenant | null;
  onSubmit: (data: any) => Promise<boolean>;
  onCancel: () => void;
  isOpen?: boolean;
}

// Form validation schema
const tenantFormSchema = z.object({
  name: z.string().min(1, { message: 'Tenant name is required' }),
  description: z.string().optional(),
  isActive: z.boolean().default(true),
  connectionString: z.string().optional(),
  maxUsers: z.coerce.number().int().positive().optional(),
  maxConnections: z.coerce.number().int().positive().optional(),
});

const TenantForm: React.FC<TenantFormProps> = ({ 
  tenant, 
  isOpen = true,
  onSubmit, 
  onCancel 
}) => {
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form setup with less strict typing
  const form = useForm({
    resolver: zodResolver(tenantFormSchema),
    defaultValues: {
      name: tenant?.name || '',
      description: tenant?.description || '',
      isActive: tenant?.isActive !== undefined ? tenant.isActive : true,
      connectionString: tenant?.connectionString || '',
      maxUsers: tenant?.maxUsers,
      maxConnections: tenant?.maxConnections,
    },
  });

  // Handle form submission
  const handleFormSubmit = async (data: any) => {
    setIsSubmitting(true);
    
    try {
      // If editing existing tenant, include the ID
      const submitData = tenant ? { ...data, id: tenant.id } : data;
      
      const success = await onSubmit(submitData);
      
      if (success) {
        toast({
          title: 'Success',
          description: tenant ? 'Tenant updated successfully' : 'Tenant created successfully',
        });
        form.reset();
        onCancel();
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to submit tenant data',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent className="sm:max-w-[550px]">
        <DialogHeader>
          <DialogTitle>
            {tenant ? 'Edit Tenant' : 'Add New Tenant'}
          </DialogTitle>
        </DialogHeader>
        
        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter tenant name" {...field} />
                </FormControl>
                <FormDescription>
                  The display name for the tenant organization
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          
          <FormField
            control={form.control}
            name="description"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Description</FormLabel>
                <FormControl>
                  <Textarea 
                    placeholder="Enter tenant description"
                    className="resize-none"
                    {...field} 
                  />
                </FormControl>
                <FormDescription>
                  Brief description of the tenant organization
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          
          <FormField
            control={form.control}
            name="connectionString"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Connection String</FormLabel>
                <FormControl>
                  <Input placeholder="Enter database connection string" {...field} />
                </FormControl>
                <FormDescription>
                  Database connection string for the tenant (optional)
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          
          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="maxUsers"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Max Users</FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      placeholder="e.g., 50" 
                      {...field}
                      value={field.value || ''} 
                      onChange={(e) => {
                        const value = e.target.value === '' ? undefined : parseInt(e.target.value, 10);
                        field.onChange(value);
                      }}
                    />
                  </FormControl>
                  <FormDescription>
                    Maximum number of users allowed
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            
            <FormField
              control={form.control}
              name="maxConnections"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Max Connections</FormLabel>
                  <FormControl>
                    <Input 
                      type="number" 
                      placeholder="e.g., 10" 
                      {...field}
                      value={field.value || ''} 
                      onChange={(e) => {
                        const value = e.target.value === '' ? undefined : parseInt(e.target.value, 10);
                        field.onChange(value);
                      }}
                    />
                  </FormControl>
                  <FormDescription>
                    Maximum concurrent connections
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <FormField
            control={form.control}
            name="isActive"
            render={({ field }) => (
              <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                <FormControl>
                  <Checkbox
                    checked={field.value}
                    onCheckedChange={field.onChange}
                  />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel>Active</FormLabel>
                  <FormDescription>
                    Determines if the tenant is currently active in the system
                  </FormDescription>
                </div>
              </FormItem>
            )}
          />

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
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {tenant ? 'Updating...' : 'Creating...'}
                </>
              ) : (
                tenant ? 'Update Tenant' : 'Create Tenant'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default TenantForm; 