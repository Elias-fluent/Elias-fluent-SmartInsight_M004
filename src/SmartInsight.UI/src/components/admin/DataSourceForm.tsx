import React, { useEffect, useState } from 'react';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { 
  Dialog, 
  DialogContent, 
  DialogHeader, 
  DialogTitle, 
  DialogFooter 
} from '../ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '../ui/form';
import { Input } from '../ui/input';
import { Button } from '../ui/button';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '../ui/select';
import { 
  Tabs, 
  TabsContent, 
  TabsList, 
  TabsTrigger 
} from '../ui/tabs';
import { Textarea } from '../ui/textarea';
import { useToast } from '../ui/use-toast';
import { useDispatch } from 'react-redux';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import ConnectionStringBuilder from './ConnectionStringBuilder';
import ScheduleConfiguration from './ScheduleConfiguration';

// Form validation schema
const formSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must be less than 100 characters'),
  description: z
    .string()
    .max(1000, 'Description must be less than 1000 characters')
    .optional(),
  sourceType: z.string().min(1, 'Data source type is required'),
  connectionString: z.string().min(1, 'Connection string is required'),
  username: z.string().optional(),
  password: z.string().optional(),
  apiKey: z.string().optional(),
  refreshInterval: z.number().min(1, 'Refresh interval must be at least 1 minute'),
  refreshScheduleType: z.enum(['minutes', 'hourly', 'daily', 'weekly', 'monthly', 'custom']),
  refreshCronExpression: z.string().optional(),
});

type FormValues = z.infer<typeof formSchema>;

interface DataSourceFormProps {
  dataSource?: any;
  onSubmit: (data: any) => Promise<boolean>;
  onCancel: () => void;
}

const DataSourceForm: React.FC<DataSourceFormProps> = ({
  dataSource,
  onSubmit,
  onCancel,
}) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [activeTab, setActiveTab] = useState('general');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [dataSourceTypes, setDataSourceTypes] = useState<Record<string, string>>({});
  const [isTestingConnection, setIsTestingConnection] = useState(false);

  const defaultValues: FormValues = {
    name: dataSource?.name || '',
    description: dataSource?.description || '',
    sourceType: dataSource?.sourceType?.toString() || '',
    connectionString: dataSource?.connectionString || '',
    username: dataSource?.username || '',
    password: '',
    apiKey: '',
    refreshInterval: dataSource?.refreshInterval || 1440, // Default to daily
    refreshScheduleType: 'daily',
    refreshCronExpression: dataSource?.refreshCronExpression || '',
  };

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues,
  });

  // Fetch data source types when component mounts
  useEffect(() => {
    const fetchDataSourceTypes = async () => {
      try {
        const response = await dispatch(apiRequest(
          '/api/v1/DataSource/types',
          'get'
        ));
        
        if (response?.payload) {
          setDataSourceTypes(response.payload);
        }
      } catch (error) {
        console.error('Failed to fetch data source types:', error);
      }
    };

    fetchDataSourceTypes();
  }, [dispatch]);

  const onFormSubmit = async (data: FormValues) => {
    setIsSubmitting(true);
    
    // Transform form data to API format
    const formattedData = {
      name: data.name,
      description: data.description,
      sourceType: parseInt(data.sourceType),
      connectionParameters: {
        connectionString: data.connectionString,
        ...(data.username && { username: data.username }),
        ...(data.password && { password: data.password }),
        ...(data.apiKey && { apiKey: data.apiKey }),
      },
      refreshSchedule: data.refreshScheduleType === 'custom' 
        ? data.refreshCronExpression 
        : toRefreshSchedule(data.refreshScheduleType, data.refreshInterval),
    };
    
    try {
      const success = await onSubmit(formattedData);
      if (success) {
        form.reset();
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const testConnection = async () => {
    // Get current form values
    const formData = form.getValues();
    
    // Create test connection parameters
    const testData = {
      sourceType: parseInt(formData.sourceType),
      connectionParameters: {
        connectionString: formData.connectionString,
        ...(formData.username && { username: formData.username }),
        ...(formData.password && { password: formData.password }),
        ...(formData.apiKey && { apiKey: formData.apiKey }),
      },
    };
    
    setIsTestingConnection(true);
    
    try {
      const response = await dispatch(apiRequest(
        '/api/v1/DataSource/test-connection',
        'post',
        testData
      ));
      
      if (response?.payload?.success) {
        toast({
          title: 'Connection Successful',
          description: 'Successfully connected to the data source',
          variant: 'default',
        });
      } else {
        toast({
          title: 'Connection Failed',
          description: response?.payload?.errorMessage || 'Could not connect to the data source',
          variant: 'destructive',
        });
      }
    } catch (error) {
      toast({
        title: 'Connection Test Failed',
        description: 'An error occurred while testing the connection',
        variant: 'destructive',
      });
    } finally {
      setIsTestingConnection(false);
    }
  };

  // Helper to convert schedule type and interval to cron expression
  const toRefreshSchedule = (scheduleType: string, interval: number): string => {
    switch (scheduleType) {
      case 'minutes':
        return `*/${interval} * * * *`; // Every N minutes
      case 'hourly':
        return `0 */${interval} * * *`; // Every N hours
      case 'daily':
        return `0 0 */${interval} * *`; // Every N days
      case 'weekly':
        return `0 0 * * ${interval % 7}`; // On day N of week
      case 'monthly':
        return `0 0 ${interval} * *`; // On day N of month
      default:
        return '0 0 * * *'; // Daily at midnight
    }
  };

  return (
    <Dialog open={true} onOpenChange={(open) => !open && onCancel()}>
      <DialogContent className="sm:max-w-[625px]">
        <DialogHeader>
          <DialogTitle>
            {dataSource ? 'Edit Data Source' : 'Add New Data Source'}
          </DialogTitle>
        </DialogHeader>
        
        <Form form={form}>
          <form onSubmit={form.handleSubmit(onFormSubmit)} className="space-y-4">
            <Tabs
              defaultValue="general"
              value={activeTab}
              onValueChange={setActiveTab}
              className="w-full"
            >
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="general">General</TabsTrigger>
                <TabsTrigger value="connection">Connection</TabsTrigger>
                <TabsTrigger value="schedule">Schedule</TabsTrigger>
              </TabsList>
              
              {/* General Settings Tab */}
              <TabsContent value="general" className="space-y-4 pt-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Name</FormLabel>
                      <FormControl>
                        <Input placeholder="Enter data source name" {...field} />
                      </FormControl>
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
                          placeholder="Enter a description of this data source" 
                          className="resize-none"
                          {...field}
                          value={field.value || ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                
                <FormField
                  control={form.control}
                  name="sourceType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Data Source Type</FormLabel>
                      <FormControl>
                        <Select
                          onValueChange={field.onChange}
                          defaultValue={field.value}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select a data source type" />
                          </SelectTrigger>
                          <SelectContent>
                            {Object.entries(dataSourceTypes).map(([value, label]) => (
                              <SelectItem key={value} value={value}>
                                {label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </TabsContent>
              
              {/* Connection Settings Tab */}
              <TabsContent value="connection" className="space-y-4 pt-4">
                <FormField
                  control={form.control}
                  name="connectionString"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Connection String</FormLabel>
                      <FormControl>
                        <div className="space-y-2">
                          <Textarea 
                            placeholder="Enter connection string"
                            className="font-mono text-sm h-24 resize-none"
                            {...field}
                          />
                          {form.watch('sourceType') && (
                            <ConnectionStringBuilder
                              dataSourceType={parseInt(form.watch('sourceType'))}
                              onChange={field.onChange}
                            />
                          )}
                        </div>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="username"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Username</FormLabel>
                        <FormControl>
                          <Input placeholder="Username (optional)" {...field} value={field.value || ''} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="password"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Password</FormLabel>
                        <FormControl>
                          <Input 
                            type="password" 
                            placeholder={dataSource ? '••••••••' : 'Password (optional)'} 
                            {...field} 
                            value={field.value || ''} 
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
                
                <FormField
                  control={form.control}
                  name="apiKey"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>API Key</FormLabel>
                      <FormControl>
                        <Input 
                          placeholder={dataSource ? '••••••••' : 'API Key (optional)'} 
                          {...field} 
                          value={field.value || ''} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={testConnection}
                  disabled={isTestingConnection || !form.watch('connectionString') || !form.watch('sourceType')}
                  className="mt-2"
                >
                  {isTestingConnection ? 'Testing...' : 'Test Connection'}
                </Button>
              </TabsContent>
              
              {/* Schedule Settings Tab */}
              <TabsContent value="schedule" className="space-y-4 pt-4">
                <ScheduleConfiguration form={form} />
              </TabsContent>
            </Tabs>
            
            <DialogFooter className="mt-6">
              <Button 
                type="button" 
                variant="secondary" 
                onClick={onCancel}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button 
                type="submit" 
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : dataSource ? 'Update' : 'Create'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};

export default DataSourceForm; 