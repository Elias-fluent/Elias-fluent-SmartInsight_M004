import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Button } from '../ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../ui/dialog';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Checkbox } from '../ui';
import { useToast } from '../ui/use-toast';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '../ui/tabs';
import { FormLabel } from '../ui/form';

// Define tenant configuration schema
const tenantConfigSchema = z.object({
  // General settings
  name: z.string().min(1, { message: 'Name is required' }),
  description: z.string().optional(),
  isActive: z.boolean().default(true),
  
  // Branding settings
  primaryColor: z.string().regex(/^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/, { 
    message: 'Must be a valid hex color (e.g. #FFFFFF)' 
  }).optional(),
  secondaryColor: z.string().regex(/^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/, { 
    message: 'Must be a valid hex color (e.g. #FFFFFF)' 
  }).optional(),
  logoUrl: z.string().url({ message: 'Must be a valid URL' }).optional().or(z.literal('')),
  
  // Feature toggles
  enabledFeatures: z.object({
    aiAssistant: z.boolean().default(true),
    dataVisualization: z.boolean().default(true),
    advancedAnalytics: z.boolean().default(false),
    customReports: z.boolean().default(false),
    knowledgeGraph: z.boolean().default(true),
    apiAccess: z.boolean().default(false),
  }),
  
  // Integration settings
  integrations: z.object({
    jiraEnabled: z.boolean().default(false),
    jiraApiUrl: z.string().url({ message: 'Must be a valid URL' }).optional().or(z.literal('')),
    jiraApiKey: z.string().optional(),
    
    confluenceEnabled: z.boolean().default(false),
    confluenceApiUrl: z.string().url({ message: 'Must be a valid URL' }).optional().or(z.literal('')),
    confluenceApiKey: z.string().optional(),
    
    gitEnabled: z.boolean().default(false),
    gitRepositoryUrl: z.string().url({ message: 'Must be a valid URL' }).optional().or(z.literal('')),
    gitAccessToken: z.string().optional(),
  }),
  
  // Limits and quotas
  limits: z.object({
    maxUsers: z.number().int().positive().default(25),
    maxDataSources: z.number().int().positive().default(10),
    maxStorageGB: z.number().int().positive().default(100),
    maxQueriesPerDay: z.number().int().positive().default(1000),
  }),
});

type TenantConfigValues = z.infer<typeof tenantConfigSchema>;

interface TenantConfigProps {
  tenantId: string;
  onClose: () => void;
}

const TenantConfig: React.FC<TenantConfigProps> = ({ tenantId, onClose }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [activeTab, setActiveTab] = useState('general');
  
  // Get the tenant from Redux store
  const tenants = useSelector((state: RootState) => state.data.tenants);
  const tenant = tenants.find(t => t.id === tenantId);
  
  // Initialize form with default values
  const form = useForm<TenantConfigValues>({
    resolver: zodResolver(tenantConfigSchema) as any,
    defaultValues: {
      name: '',
      description: '',
      isActive: true,
      primaryColor: '#1a56db',
      secondaryColor: '#7e3af2',
      logoUrl: '',
      enabledFeatures: {
        aiAssistant: true,
        dataVisualization: true,
        advancedAnalytics: false,
        customReports: false,
        knowledgeGraph: true,
        apiAccess: false,
      },
      integrations: {
        jiraEnabled: false,
        jiraApiUrl: '',
        jiraApiKey: '',
        confluenceEnabled: false,
        confluenceApiUrl: '',
        confluenceApiKey: '',
        gitEnabled: false,
        gitRepositoryUrl: '',
        gitAccessToken: '',
      },
      limits: {
        maxUsers: 25,
        maxDataSources: 10,
        maxStorageGB: 100,
        maxQueriesPerDay: 1000,
      }
    }
  });
  
  // Fetch tenant configuration when component mounts
  useEffect(() => {
    const fetchTenantConfig = async () => {
      if (!tenantId) return;
      
      setIsLoading(true);
      try {
        const response = await dispatch(apiRequest(
          `/api/v1/Tenants/${tenantId}/config`,
          'get'
        ));
        
        if (response?.payload) {
          // Merge tenant basic info with config
          const config = {
            ...response.payload,
            name: tenant?.name || '',
            description: tenant?.description || '',
            isActive: tenant?.isActive !== undefined ? tenant.isActive : true,
          };
          
          // Reset form with fetched values
          form.reset(config);
        }
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to fetch tenant configuration',
          variant: 'destructive',
        });
        
        // If config doesn't exist yet, initialize with tenant basic info
        if (tenant) {
          form.setValue('name', tenant.name);
          form.setValue('description', tenant.description || '');
          form.setValue('isActive', tenant.isActive);
        }
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchTenantConfig();
  }, [tenantId, dispatch, tenant]);
  
  // Handle form submission
  const handleSubmit = async (data: TenantConfigValues) => {
    if (!tenantId) return;
    
    setIsSubmitting(true);
    try {
      // Update tenant basic info
      await dispatch(apiRequest(
        `/api/v1/Tenants/${tenantId}`,
        'put',
        {
          name: data.name,
          description: data.description,
          isActive: data.isActive,
        }
      ));
      
      // Update tenant configuration
      await dispatch(apiRequest(
        `/api/v1/Tenants/${tenantId}/config`,
        'put',
        {
          primaryColor: data.primaryColor,
          secondaryColor: data.secondaryColor,
          logoUrl: data.logoUrl,
          enabledFeatures: data.enabledFeatures,
          integrations: data.integrations,
          limits: data.limits,
        }
      ));
      
      toast({
        title: 'Success',
        description: 'Tenant configuration saved successfully',
      });
      
      onClose();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save tenant configuration',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };
  
  // Handle form reset
  const handleReset = () => {
    form.reset();
    
    toast({
      title: 'Form Reset',
      description: 'All changes have been discarded',
    });
  };
  
  // Generate feature toggle controls
  const renderFeatureToggles = () => {
    const features = [
      { id: 'aiAssistant', label: 'AI Assistant', description: 'Enable AI-powered chat assistant for this tenant' },
      { id: 'dataVisualization', label: 'Data Visualization', description: 'Enable charts and visualization tools' },
      { id: 'advancedAnalytics', label: 'Advanced Analytics', description: 'Enable complex analytics and reporting features' },
      { id: 'customReports', label: 'Custom Reports', description: 'Allow users to create and save custom reports' },
      { id: 'knowledgeGraph', label: 'Knowledge Graph', description: 'Enable knowledge graph creation and visualization' },
      { id: 'apiAccess', label: 'API Access', description: 'Enable programmatic access to data via APIs' },
    ];
    
    return (
      <div className="space-y-4">
        {features.map(feature => {
          // Use a type-safe approach to access nested form values
          const fieldName = `enabledFeatures.${feature.id}` as const;
          const isEnabled = form.watch(fieldName as any);
          
          return (
            <div key={feature.id} className="flex items-start space-x-2">
              <Checkbox
                id={`feature-${feature.id}`}
                checked={isEnabled}
                onCheckedChange={(checked) => 
                  form.setValue(fieldName as any, Boolean(checked), { shouldValidate: true })
                }
              />
              <div className="space-y-1">
                <FormLabel htmlFor={`feature-${feature.id}`} className="font-medium cursor-pointer">
                  {feature.label}
                </FormLabel>
                <p className="text-xs text-gray-500">{feature.description}</p>
              </div>
            </div>
          );
        })}
      </div>
    );
  };
  
  if (isLoading) {
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
          <DialogTitle>
            Tenant Configuration: {tenant?.name}
          </DialogTitle>
        </DialogHeader>
        
        <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
          <Tabs defaultValue="general" value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="mb-4">
              <TabsTrigger value="general">General</TabsTrigger>
              <TabsTrigger value="branding">Branding</TabsTrigger>
              <TabsTrigger value="features">Features</TabsTrigger>
              <TabsTrigger value="integrations">Integrations</TabsTrigger>
              <TabsTrigger value="limits">Limits & Quotas</TabsTrigger>
            </TabsList>
            
            {/* General Settings */}
            <TabsContent value="general" className="space-y-4">
              <div className="space-y-2">
                <FormLabel htmlFor="name">Tenant Name</FormLabel>
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
            </TabsContent>
            
            {/* Branding Settings */}
            <TabsContent value="branding" className="space-y-4">
              <div className="space-y-2">
                <FormLabel htmlFor="primaryColor">Primary Color</FormLabel>
                <div className="flex items-center space-x-2">
                  <Input 
                    id="primaryColor"
                    placeholder="#1a56db" 
                    {...form.register('primaryColor')} 
                  />
                  <div 
                    className="w-10 h-10 rounded border"
                    style={{ backgroundColor: form.watch('primaryColor') || '#1a56db' }}
                  ></div>
                </div>
                {form.formState.errors.primaryColor && (
                  <p className="text-sm text-red-500">{form.formState.errors.primaryColor.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="secondaryColor">Secondary Color</FormLabel>
                <div className="flex items-center space-x-2">
                  <Input 
                    id="secondaryColor"
                    placeholder="#7e3af2" 
                    {...form.register('secondaryColor')} 
                  />
                  <div 
                    className="w-10 h-10 rounded border"
                    style={{ backgroundColor: form.watch('secondaryColor') || '#7e3af2' }}
                  ></div>
                </div>
                {form.formState.errors.secondaryColor && (
                  <p className="text-sm text-red-500">{form.formState.errors.secondaryColor.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="logoUrl">Logo URL</FormLabel>
                <Input 
                  id="logoUrl"
                  placeholder="https://example.com/logo.png" 
                  {...form.register('logoUrl')} 
                />
                {form.formState.errors.logoUrl && (
                  <p className="text-sm text-red-500">{form.formState.errors.logoUrl.message}</p>
                )}
              </div>
            </TabsContent>
            
            {/* Feature Toggles */}
            <TabsContent value="features" className="space-y-4">
              <p className="text-sm text-gray-500 mb-4">
                Enable or disable features for this tenant. Disabled features will not be accessible to users.
              </p>
              {renderFeatureToggles()}
            </TabsContent>
            
            {/* Integration Settings */}
            <TabsContent value="integrations" className="space-y-6">
              {/* JIRA Integration */}
              <div className="border p-4 rounded-md space-y-3">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="jiraEnabled"
                    checked={form.watch('integrations.jiraEnabled')}
                    onCheckedChange={(checked) => 
                      form.setValue('integrations.jiraEnabled', checked as boolean)
                    }
                  />
                  <FormLabel htmlFor="jiraEnabled" className="font-medium cursor-pointer">
                    JIRA Integration
                  </FormLabel>
                </div>
                
                {form.watch('integrations.jiraEnabled') && (
                  <div className="ml-6 space-y-3">
                    <div className="space-y-2">
                      <FormLabel htmlFor="jiraApiUrl">JIRA API URL</FormLabel>
                      <Input 
                        id="jiraApiUrl"
                        placeholder="https://your-domain.atlassian.net/rest/api/3" 
                        {...form.register('integrations.jiraApiUrl')} 
                      />
                      {form.formState.errors.integrations?.jiraApiUrl && (
                        <p className="text-sm text-red-500">{form.formState.errors.integrations.jiraApiUrl.message}</p>
                      )}
                    </div>
                    
                    <div className="space-y-2">
                      <FormLabel htmlFor="jiraApiKey">JIRA API Key</FormLabel>
                      <Input 
                        id="jiraApiKey"
                        type="password"
                        placeholder="API Key" 
                        {...form.register('integrations.jiraApiKey')} 
                      />
                    </div>
                  </div>
                )}
              </div>
              
              {/* Confluence Integration */}
              <div className="border p-4 rounded-md space-y-3">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="confluenceEnabled"
                    checked={form.watch('integrations.confluenceEnabled')}
                    onCheckedChange={(checked) => 
                      form.setValue('integrations.confluenceEnabled', checked as boolean)
                    }
                  />
                  <FormLabel htmlFor="confluenceEnabled" className="font-medium cursor-pointer">
                    Confluence Integration
                  </FormLabel>
                </div>
                
                {form.watch('integrations.confluenceEnabled') && (
                  <div className="ml-6 space-y-3">
                    <div className="space-y-2">
                      <FormLabel htmlFor="confluenceApiUrl">Confluence API URL</FormLabel>
                      <Input 
                        id="confluenceApiUrl"
                        placeholder="https://your-domain.atlassian.net/wiki/rest/api" 
                        {...form.register('integrations.confluenceApiUrl')} 
                      />
                      {form.formState.errors.integrations?.confluenceApiUrl && (
                        <p className="text-sm text-red-500">{form.formState.errors.integrations.confluenceApiUrl.message}</p>
                      )}
                    </div>
                    
                    <div className="space-y-2">
                      <FormLabel htmlFor="confluenceApiKey">Confluence API Key</FormLabel>
                      <Input 
                        id="confluenceApiKey"
                        type="password"
                        placeholder="API Key" 
                        {...form.register('integrations.confluenceApiKey')} 
                      />
                    </div>
                  </div>
                )}
              </div>
              
              {/* Git Integration */}
              <div className="border p-4 rounded-md space-y-3">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="gitEnabled"
                    checked={form.watch('integrations.gitEnabled')}
                    onCheckedChange={(checked) => 
                      form.setValue('integrations.gitEnabled', checked as boolean)
                    }
                  />
                  <FormLabel htmlFor="gitEnabled" className="font-medium cursor-pointer">
                    Git Repository Integration
                  </FormLabel>
                </div>
                
                {form.watch('integrations.gitEnabled') && (
                  <div className="ml-6 space-y-3">
                    <div className="space-y-2">
                      <FormLabel htmlFor="gitRepositoryUrl">Repository URL</FormLabel>
                      <Input 
                        id="gitRepositoryUrl"
                        placeholder="https://github.com/organization/repo" 
                        {...form.register('integrations.gitRepositoryUrl')} 
                      />
                      {form.formState.errors.integrations?.gitRepositoryUrl && (
                        <p className="text-sm text-red-500">{form.formState.errors.integrations.gitRepositoryUrl.message}</p>
                      )}
                    </div>
                    
                    <div className="space-y-2">
                      <FormLabel htmlFor="gitAccessToken">Access Token</FormLabel>
                      <Input 
                        id="gitAccessToken"
                        type="password"
                        placeholder="Access Token" 
                        {...form.register('integrations.gitAccessToken')} 
                      />
                    </div>
                  </div>
                )}
              </div>
            </TabsContent>
            
            {/* Limits & Quotas */}
            <TabsContent value="limits" className="space-y-4">
              <p className="text-sm text-gray-500 mb-4">
                Configure resource limits and usage quotas for this tenant.
              </p>
              
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <FormLabel htmlFor="maxUsers">Maximum Users</FormLabel>
                  <Input 
                    id="maxUsers"
                    type="number"
                    min="1"
                    {...form.register('limits.maxUsers', { valueAsNumber: true })} 
                  />
                  {form.formState.errors.limits?.maxUsers && (
                    <p className="text-sm text-red-500">{form.formState.errors.limits.maxUsers.message}</p>
                  )}
                </div>
                
                <div className="space-y-2">
                  <FormLabel htmlFor="maxDataSources">Maximum Data Sources</FormLabel>
                  <Input 
                    id="maxDataSources"
                    type="number"
                    min="1"
                    {...form.register('limits.maxDataSources', { valueAsNumber: true })} 
                  />
                  {form.formState.errors.limits?.maxDataSources && (
                    <p className="text-sm text-red-500">{form.formState.errors.limits.maxDataSources.message}</p>
                  )}
                </div>
                
                <div className="space-y-2">
                  <FormLabel htmlFor="maxStorageGB">Storage Limit (GB)</FormLabel>
                  <Input 
                    id="maxStorageGB"
                    type="number"
                    min="1"
                    {...form.register('limits.maxStorageGB', { valueAsNumber: true })} 
                  />
                  {form.formState.errors.limits?.maxStorageGB && (
                    <p className="text-sm text-red-500">{form.formState.errors.limits.maxStorageGB.message}</p>
                  )}
                </div>
                
                <div className="space-y-2">
                  <FormLabel htmlFor="maxQueriesPerDay">Maximum Queries Per Day</FormLabel>
                  <Input 
                    id="maxQueriesPerDay"
                    type="number"
                    min="1"
                    {...form.register('limits.maxQueriesPerDay', { valueAsNumber: true })} 
                  />
                  {form.formState.errors.limits?.maxQueriesPerDay && (
                    <p className="text-sm text-red-500">{form.formState.errors.limits.maxQueriesPerDay.message}</p>
                  )}
                </div>
              </div>
            </TabsContent>
          </Tabs>
          
          <DialogFooter className="flex justify-between">
            <div>
              <Button 
                type="button" 
                variant="outline" 
                onClick={handleReset}
                disabled={isSubmitting}
              >
                Reset
              </Button>
            </div>
            <div className="space-x-2">
              <Button 
                type="button" 
                variant="outline" 
                onClick={onClose}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button 
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Configuration'}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default TenantConfig; 