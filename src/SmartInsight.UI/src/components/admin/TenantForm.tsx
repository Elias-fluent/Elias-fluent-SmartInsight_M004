import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Button, Input, FormLabel } from '../ui';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../ui/dialog';
import {
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '../ui/form';
import { Textarea } from '../ui/textarea';
import { Checkbox } from '../ui';
import { useToast } from '../ui/use-toast';

interface Tenant {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
}

interface TenantFormProps {
  tenant: Tenant | null;
  onSubmit: (data: any) => Promise<boolean>;
  onCancel: () => void;
}

// Form validation schema
const tenantFormSchema = z.object({
  name: z.string().min(1, { message: 'Name is required' }),
  description: z.string().optional(),
  isActive: z.boolean().default(true),
});

// Define the form data type based on the schema
type TenantFormValues = z.infer<typeof tenantFormSchema>;

const TenantForm: React.FC<TenantFormProps> = ({ tenant, onSubmit, onCancel }) => {
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form setup
  const form = useForm<TenantFormValues>({
    resolver: zodResolver(tenantFormSchema) as any,
    defaultValues: {
      name: tenant?.name || '',
      description: tenant?.description || '',
      isActive: tenant?.isActive !== undefined ? tenant.isActive : true,
    },
  });

  // Handle form submission
  const handleFormSubmit = async (data: TenantFormValues) => {
    setIsSubmitting(true);
    
    try {
      const success = await onSubmit(data);
      if (success) {
        form.reset();
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
    <Dialog open={true} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {tenant ? 'Edit Tenant' : 'Add New Tenant'}
          </DialogTitle>
        </DialogHeader>
        
        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-4">
          <div className="space-y-4">
            <div className="space-y-2">
              <FormLabel htmlFor="name">Name</FormLabel>
              <Input 
                id="name"
                placeholder="Tenant name" 
                {...form.register('name')} 
              />
              {form.formState.errors.name && (
                <p className="text-sm text-red-500">{form.formState.errors.name.message}</p>
              )}
            </div>
            
            <div className="space-y-2">
              <FormLabel htmlFor="description">Description</FormLabel>
              <Textarea 
                id="description"
                placeholder="Describe the tenant's purpose or organization"
                className="resize-none"
                {...form.register('description')} 
              />
              {form.formState.errors.description && (
                <p className="text-sm text-red-500">{form.formState.errors.description.message}</p>
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
              {isSubmitting ? 'Saving...' : tenant ? 'Update Tenant' : 'Create Tenant'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default TenantForm; 