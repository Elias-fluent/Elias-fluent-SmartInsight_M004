import React, { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useToast } from '../ui/use-toast';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { 
  Dialog, 
  DialogContent, 
  DialogHeader, 
  DialogTitle, 
  DialogFooter 
} from '../ui/dialog';
import { Button } from '../ui/button';
import { 
  Input,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage
} from '../ui';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '../ui/tabs';
import { Checkbox } from '../ui/checkbox';
import { Database, Settings, Shield, Clock, HardDrive } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Textarea } from '../ui/textarea';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';

interface TenantConfigProps {
  tenantId: string;
  onClose: () => void;
}

interface TenantSettings {
  id: string;
  name: string;
  dataRetentionDays: number;
  maxStorageGB: number;
  refreshScheduleType: 'minutes' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'custom';
  refreshInterval: number;
  enableAuditLogs: boolean;
  customSettings: Record<string, any>;
}

const TenantConfig: React.FC<TenantConfigProps> = ({ tenantId, onClose }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [activeTab, setActiveTab] = useState('general');
  const [settings, setSettings] = useState<TenantSettings>({
    id: tenantId,
    name: '',
    dataRetentionDays: 30,
    maxStorageGB: 100,
    refreshScheduleType: 'daily',
    refreshInterval: 1,
    enableAuditLogs: true,
    customSettings: {}
  });

  // Get tenant from Redux store
  const tenant = useSelector((state: RootState) => 
    state.data.tenants.find(t => t.id === tenantId)
  );

  // Fetch tenant settings on mount
  useEffect(() => {
    fetchTenantSettings();
  }, [tenantId]);

  const fetchTenantSettings = async () => {
    setIsLoading(true);
    try {
      // Placeholder for actual API call - would be implemented in real app
      // const response = await dispatch(apiRequest(
      //   `/api/v1/Tenants/${tenantId}/settings`,
      //   'get',
      //   undefined,
      //   'FETCH_TENANT_SETTINGS_SUCCESS',
      //   'FETCH_TENANT_SETTINGS_FAILURE'
      // ));
      
      // For now, just use the tenant name from Redux
      if (tenant) {
        setSettings(prev => ({
          ...prev,
          id: tenantId,
          name: tenant.name
        }));
      }

      // Simulate an API delay
      setTimeout(() => {
        setIsLoading(false);
      }, 500);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch tenant settings',
        variant: 'destructive',
      });
      setIsLoading(false);
    }
  };

  const handleSaveSettings = async () => {
    setIsSaving(true);
    try {
      // Placeholder for actual API call - would be implemented in real app
      // await dispatch(apiRequest(
      //   `/api/v1/Tenants/${tenantId}/settings`,
      //   'put',
      //   settings,
      //   'UPDATE_TENANT_SETTINGS_SUCCESS',
      //   'UPDATE_TENANT_SETTINGS_FAILURE'
      // ));
      
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 800));
      
      toast({
        title: 'Success',
        description: 'Tenant settings updated successfully',
      });
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to update tenant settings',
        variant: 'destructive',
      });
    } finally {
      setIsSaving(false);
    }
  };

  const handleInputChange = (field: keyof TenantSettings, value: any) => {
    setSettings(prev => ({
      ...prev,
      [field]: value
    }));
  };

  return (
    <Dialog open={true} onOpenChange={() => onClose()}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            <span>Configure Tenant: {tenant?.name || 'Loading...'}</span>
          </DialogTitle>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center items-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
          </div>
        ) : (
          <>
            <Tabs defaultValue="general" value={activeTab} onValueChange={setActiveTab}>
              <TabsList className="grid grid-cols-3 mb-6">
                <TabsTrigger value="general" className="flex items-center gap-2">
                  <Settings className="h-4 w-4" />
                  <span>General</span>
                </TabsTrigger>
                <TabsTrigger value="data" className="flex items-center gap-2">
                  <Database className="h-4 w-4" />
                  <span>Data Settings</span>
                </TabsTrigger>
                <TabsTrigger value="schedule" className="flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  <span>Schedule</span>
                </TabsTrigger>
              </TabsList>

              <TabsContent value="general" className="space-y-4">
                <FormItem>
                  <FormLabel>Tenant Name</FormLabel>
                  <FormControl>
                    <Input 
                      value={settings.name}
                      onChange={(e) => handleInputChange('name', e.target.value)}
                      disabled={true} // Name is read-only here
                    />
                  </FormControl>
                  <FormDescription>
                    The display name of the tenant (edit in tenant details)
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Enable Audit Logs</FormLabel>
                  <div className="flex items-center space-x-2 mt-2">
                    <Checkbox
                      id="enable-audit-logs"
                      checked={settings.enableAuditLogs}
                      onCheckedChange={(checked) => 
                        handleInputChange('enableAuditLogs', Boolean(checked))
                      }
                    />
                    <FormLabel htmlFor="enable-audit-logs" className="cursor-pointer font-normal">
                      Enable detailed audit logging for this tenant
                    </FormLabel>
                  </div>
                  <FormDescription>
                    Track all user actions and system events for compliance purposes
                  </FormDescription>
                </FormItem>
              </TabsContent>

              <TabsContent value="data" className="space-y-4">
                <FormItem>
                  <FormLabel>Data Retention Period (Days)</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={1}
                      max={365}
                      value={settings.dataRetentionDays}
                      onChange={(e) => handleInputChange('dataRetentionDays', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    Number of days to retain data before automatic purging
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Maximum Storage (GB)</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={1}
                      value={settings.maxStorageGB}
                      onChange={(e) => handleInputChange('maxStorageGB', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    Maximum storage allocation for this tenant in gigabytes
                  </FormDescription>
                </FormItem>
              </TabsContent>

              <TabsContent value="schedule" className="space-y-4">
                <FormItem>
                  <FormLabel>Refresh Schedule Type</FormLabel>
                  <Select 
                    value={settings.refreshScheduleType}
                    onValueChange={(value) => handleInputChange('refreshScheduleType', value)}
                  >
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select schedule type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="minutes">Minutes</SelectItem>
                      <SelectItem value="hourly">Hourly</SelectItem>
                      <SelectItem value="daily">Daily</SelectItem>
                      <SelectItem value="weekly">Weekly</SelectItem>
                      <SelectItem value="monthly">Monthly</SelectItem>
                      <SelectItem value="custom">Custom</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormDescription>
                    How frequently to refresh data sources
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Refresh Interval</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={1}
                      value={settings.refreshInterval}
                      onChange={(e) => handleInputChange('refreshInterval', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    {settings.refreshScheduleType === 'minutes' ? 'Minutes between refreshes' :
                     settings.refreshScheduleType === 'hourly' ? 'Hours between refreshes' :
                     settings.refreshScheduleType === 'daily' ? 'Days between refreshes' :
                     settings.refreshScheduleType === 'weekly' ? 'Weeks between refreshes' :
                     settings.refreshScheduleType === 'monthly' ? 'Months between refreshes' :
                     'Custom interval (advanced)'}
                  </FormDescription>
                </FormItem>
              </TabsContent>
            </Tabs>

            <DialogFooter className="mt-6">
              <Button 
                variant="outline" 
                onClick={onClose}
                disabled={isSaving}
              >
                Cancel
              </Button>
              <Button 
                onClick={handleSaveSettings}
                disabled={isSaving}
              >
                {isSaving ? 'Saving...' : 'Save Settings'}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default TenantConfig; 