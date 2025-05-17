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
import { Shield, Lock, AlertCircle, Network } from 'lucide-react';

// Define security settings schema
const securitySettingsSchema = z.object({
  // Password Policy
  passwordPolicy: z.object({
    minLength: z.number().min(6).max(32).default(8),
    requireUppercase: z.boolean().default(true),
    requireLowercase: z.boolean().default(true),
    requireNumbers: z.boolean().default(true),
    requireSpecialChars: z.boolean().default(true),
    preventPasswordReuse: z.boolean().default(true),
    passwordHistoryCount: z.number().min(1).max(24).default(5),
    passwordExpiryDays: z.number().min(0).max(365).default(90)
  }),
  
  // Authentication Settings
  authSettings: z.object({
    maxFailedAttempts: z.number().min(1).max(10).default(3),
    lockoutDurationMinutes: z.number().min(1).max(1440).default(30),
    sessionTimeoutMinutes: z.number().min(1).max(1440).default(60),
    allowedOrigins: z.string().optional(),
    requireTwoFactor: z.boolean().default(false),
    allowRememberMe: z.boolean().default(true),
    tokenExpirationMinutes: z.number().min(5).max(1440).default(60)
  }),
  
  // IP Restrictions
  ipRestrictions: z.object({
    enableIpRestrictions: z.boolean().default(false),
    allowedIpRanges: z.string().optional(),
    denyListEnabled: z.boolean().default(false),
    deniedIpRanges: z.string().optional(),
    notifyOnUnauthorizedAccess: z.boolean().default(true)
  }),
  
  // Security Alerts
  securityAlerts: z.object({
    alertOnFailedLogins: z.boolean().default(true),
    alertOnPasswordChanges: z.boolean().default(true),
    alertOnRoleChanges: z.boolean().default(true),
    alertOnNewUserCreation: z.boolean().default(true),
    alertDestinationEmail: z.string().email().optional()
  })
});

type SecuritySettingsValues = z.infer<typeof securitySettingsSchema>;

interface TenantSecuritySettingsProps {
  tenantId: string;
  onClose: () => void;
}

const TenantSecuritySettings: React.FC<TenantSecuritySettingsProps> = ({ tenantId, onClose }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [activeTab, setActiveTab] = useState('passwordPolicy');
  
  // Get the tenant from Redux store
  const tenants = useSelector((state: RootState) => state.data.tenants);
  const tenant = tenants.find(t => t.id === tenantId);
  
  // Initialize form with default values
  const form = useForm<SecuritySettingsValues>({
    resolver: zodResolver(securitySettingsSchema) as any,
    defaultValues: {
      passwordPolicy: {
        minLength: 8,
        requireUppercase: true,
        requireLowercase: true,
        requireNumbers: true,
        requireSpecialChars: true,
        preventPasswordReuse: true,
        passwordHistoryCount: 5,
        passwordExpiryDays: 90
      },
      authSettings: {
        maxFailedAttempts: 3,
        lockoutDurationMinutes: 30,
        sessionTimeoutMinutes: 60,
        allowedOrigins: '',
        requireTwoFactor: false,
        allowRememberMe: true,
        tokenExpirationMinutes: 60
      },
      ipRestrictions: {
        enableIpRestrictions: false,
        allowedIpRanges: '',
        denyListEnabled: false,
        deniedIpRanges: '',
        notifyOnUnauthorizedAccess: true
      },
      securityAlerts: {
        alertOnFailedLogins: true,
        alertOnPasswordChanges: true,
        alertOnRoleChanges: true,
        alertOnNewUserCreation: true,
        alertDestinationEmail: ''
      }
    }
  });
  
  // Fetch security settings when component mounts
  useEffect(() => {
    const fetchSecuritySettings = async () => {
      if (!tenantId) return;
      
      setIsLoading(true);
      try {
        const response = await dispatch(apiRequest(
          `/api/v1/Tenants/${tenantId}/security`,
          'get'
        ));
        
        if (response?.payload) {
          // Reset form with fetched values
          form.reset(response.payload);
        }
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to fetch security settings',
          variant: 'destructive',
        });
        
        // Keep default values if fetch fails
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchSecuritySettings();
  }, [tenantId, dispatch, form]);
  
  // Handle form submission
  const handleSubmit = async (data: SecuritySettingsValues) => {
    if (!tenantId) return;
    
    setIsSubmitting(true);
    try {
      // Update security settings
      await dispatch(apiRequest(
        `/api/v1/Tenants/${tenantId}/security`,
        'put',
        data
      ));
      
      toast({
        title: 'Success',
        description: 'Security settings saved successfully',
      });
      
      onClose();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save security settings',
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
            <div className="flex items-center gap-2">
              <Shield className="h-5 w-5" />
              <span>Security Settings: {tenant?.name}</span>
            </div>
          </DialogTitle>
        </DialogHeader>
        
        <div className="text-sm text-yellow-600 bg-yellow-50 p-3 rounded-md flex items-start mb-4">
          <AlertCircle className="h-5 w-5 mr-2 flex-shrink-0 mt-0.5" />
          <p>Security settings affect all users within this tenant. Some changes may require users to re-authenticate.</p>
        </div>
        
        <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
          <Tabs defaultValue="passwordPolicy" value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="mb-4 grid grid-cols-4 w-full">
              <TabsTrigger value="passwordPolicy" className="flex items-center gap-2">
                <Lock className="h-4 w-4" />
                <span>Password Policy</span>
              </TabsTrigger>
              <TabsTrigger value="authSettings" className="flex items-center gap-2">
                <Shield className="h-4 w-4" />
                <span>Authentication</span>
              </TabsTrigger>
              <TabsTrigger value="ipRestrictions" className="flex items-center gap-2">
                <Network className="h-4 w-4" />
                <span>IP Restrictions</span>
              </TabsTrigger>
              <TabsTrigger value="securityAlerts" className="flex items-center gap-2">
                <AlertCircle className="h-4 w-4" />
                <span>Alerts</span>
              </TabsTrigger>
            </TabsList>
            
            {/* Password Policy Tab */}
            <TabsContent value="passwordPolicy" className="space-y-4">
              <div className="space-y-2">
                <FormLabel htmlFor="minLength">Minimum Password Length</FormLabel>
                <Input 
                  id="minLength"
                  type="number"
                  min="6"
                  max="32"
                  {...form.register('passwordPolicy.minLength', { valueAsNumber: true })} 
                />
                {form.formState.errors.passwordPolicy?.minLength && (
                  <p className="text-sm text-red-500">{form.formState.errors.passwordPolicy.minLength.message}</p>
                )}
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="requireUppercase"
                    checked={form.watch('passwordPolicy.requireUppercase')}
                    onCheckedChange={(checked) => 
                      form.setValue('passwordPolicy.requireUppercase', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="requireUppercase" className="cursor-pointer">
                    Require uppercase letters
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="requireLowercase"
                    checked={form.watch('passwordPolicy.requireLowercase')}
                    onCheckedChange={(checked) => 
                      form.setValue('passwordPolicy.requireLowercase', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="requireLowercase" className="cursor-pointer">
                    Require lowercase letters
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="requireNumbers"
                    checked={form.watch('passwordPolicy.requireNumbers')}
                    onCheckedChange={(checked) => 
                      form.setValue('passwordPolicy.requireNumbers', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="requireNumbers" className="cursor-pointer">
                    Require numbers
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="requireSpecialChars"
                    checked={form.watch('passwordPolicy.requireSpecialChars')}
                    onCheckedChange={(checked) => 
                      form.setValue('passwordPolicy.requireSpecialChars', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="requireSpecialChars" className="cursor-pointer">
                    Require special characters
                  </FormLabel>
                </div>
              </div>
              
              <div className="flex items-center space-x-2 pt-2">
                <Checkbox
                  id="preventPasswordReuse"
                  checked={form.watch('passwordPolicy.preventPasswordReuse')}
                  onCheckedChange={(checked) => 
                    form.setValue('passwordPolicy.preventPasswordReuse', Boolean(checked))
                  }
                />
                <FormLabel htmlFor="preventPasswordReuse" className="cursor-pointer">
                  Prevent password reuse
                </FormLabel>
              </div>
              
              {form.watch('passwordPolicy.preventPasswordReuse') && (
                <div className="space-y-2 ml-6">
                  <FormLabel htmlFor="passwordHistoryCount">Password history count</FormLabel>
                  <Input 
                    id="passwordHistoryCount"
                    type="number"
                    min="1"
                    max="24"
                    {...form.register('passwordPolicy.passwordHistoryCount', { valueAsNumber: true })} 
                  />
                  {form.formState.errors.passwordPolicy?.passwordHistoryCount && (
                    <p className="text-sm text-red-500">{form.formState.errors.passwordPolicy.passwordHistoryCount.message}</p>
                  )}
                </div>
              )}
              
              <div className="space-y-2 pt-2">
                <FormLabel htmlFor="passwordExpiryDays">Password Expiry (days, 0 = never expire)</FormLabel>
                <Input 
                  id="passwordExpiryDays"
                  type="number"
                  min="0"
                  max="365"
                  {...form.register('passwordPolicy.passwordExpiryDays', { valueAsNumber: true })} 
                />
                {form.formState.errors.passwordPolicy?.passwordExpiryDays && (
                  <p className="text-sm text-red-500">{form.formState.errors.passwordPolicy.passwordExpiryDays.message}</p>
                )}
              </div>
            </TabsContent>
            
            {/* Authentication Settings Tab */}
            <TabsContent value="authSettings" className="space-y-4">
              <div className="space-y-2">
                <FormLabel htmlFor="maxFailedAttempts">Max Failed Login Attempts</FormLabel>
                <Input 
                  id="maxFailedAttempts"
                  type="number"
                  min="1"
                  max="10"
                  {...form.register('authSettings.maxFailedAttempts', { valueAsNumber: true })} 
                />
                {form.formState.errors.authSettings?.maxFailedAttempts && (
                  <p className="text-sm text-red-500">{form.formState.errors.authSettings.maxFailedAttempts.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="lockoutDurationMinutes">Account Lockout Duration (minutes)</FormLabel>
                <Input 
                  id="lockoutDurationMinutes"
                  type="number"
                  min="1"
                  max="1440"
                  {...form.register('authSettings.lockoutDurationMinutes', { valueAsNumber: true })} 
                />
                {form.formState.errors.authSettings?.lockoutDurationMinutes && (
                  <p className="text-sm text-red-500">{form.formState.errors.authSettings.lockoutDurationMinutes.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="sessionTimeoutMinutes">Session Timeout (minutes)</FormLabel>
                <Input 
                  id="sessionTimeoutMinutes"
                  type="number"
                  min="1"
                  max="1440"
                  {...form.register('authSettings.sessionTimeoutMinutes', { valueAsNumber: true })} 
                />
                {form.formState.errors.authSettings?.sessionTimeoutMinutes && (
                  <p className="text-sm text-red-500">{form.formState.errors.authSettings.sessionTimeoutMinutes.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="tokenExpirationMinutes">Token Expiration (minutes)</FormLabel>
                <Input 
                  id="tokenExpirationMinutes"
                  type="number"
                  min="5"
                  max="1440"
                  {...form.register('authSettings.tokenExpirationMinutes', { valueAsNumber: true })} 
                />
                {form.formState.errors.authSettings?.tokenExpirationMinutes && (
                  <p className="text-sm text-red-500">{form.formState.errors.authSettings.tokenExpirationMinutes.message}</p>
                )}
              </div>
              
              <div className="space-y-2">
                <FormLabel htmlFor="allowedOrigins">Allowed Origins (CORS)</FormLabel>
                <Textarea 
                  id="allowedOrigins"
                  placeholder="https://example.com,https://sub.example.com"
                  className="resize-none"
                  {...form.register('authSettings.allowedOrigins')} 
                />
                <p className="text-xs text-gray-500">Comma-separated list of allowed origins</p>
              </div>
              
              <div className="grid grid-cols-2 gap-4 pt-2">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="requireTwoFactor"
                    checked={form.watch('authSettings.requireTwoFactor')}
                    onCheckedChange={(checked) => 
                      form.setValue('authSettings.requireTwoFactor', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="requireTwoFactor" className="cursor-pointer">
                    Require two-factor authentication
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="allowRememberMe"
                    checked={form.watch('authSettings.allowRememberMe')}
                    onCheckedChange={(checked) => 
                      form.setValue('authSettings.allowRememberMe', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="allowRememberMe" className="cursor-pointer">
                    Allow "Remember Me" option
                  </FormLabel>
                </div>
              </div>
            </TabsContent>
            
            {/* IP Restrictions Tab */}
            <TabsContent value="ipRestrictions" className="space-y-4">
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="enableIpRestrictions"
                  checked={form.watch('ipRestrictions.enableIpRestrictions')}
                  onCheckedChange={(checked) => 
                    form.setValue('ipRestrictions.enableIpRestrictions', Boolean(checked))
                  }
                />
                <FormLabel htmlFor="enableIpRestrictions" className="cursor-pointer font-medium">
                  Enable IP restrictions
                </FormLabel>
              </div>
              
              {form.watch('ipRestrictions.enableIpRestrictions') && (
                <>
                  <div className="space-y-2 ml-6">
                    <FormLabel htmlFor="allowedIpRanges">Allowed IP Ranges</FormLabel>
                    <Textarea 
                      id="allowedIpRanges"
                      placeholder="192.168.1.0/24,10.0.0.0/8,203.0.113.45"
                      className="resize-none"
                      {...form.register('ipRestrictions.allowedIpRanges')} 
                    />
                    <p className="text-xs text-gray-500">Enter IP addresses or CIDR ranges, separated by commas</p>
                  </div>
                  
                  <div className="flex items-center space-x-2 ml-6">
                    <Checkbox
                      id="denyListEnabled"
                      checked={form.watch('ipRestrictions.denyListEnabled')}
                      onCheckedChange={(checked) => 
                        form.setValue('ipRestrictions.denyListEnabled', Boolean(checked))
                      }
                    />
                    <FormLabel htmlFor="denyListEnabled" className="cursor-pointer">
                      Enable IP deny list
                    </FormLabel>
                  </div>
                  
                  {form.watch('ipRestrictions.denyListEnabled') && (
                    <div className="space-y-2 ml-12">
                      <FormLabel htmlFor="deniedIpRanges">Denied IP Ranges</FormLabel>
                      <Textarea 
                        id="deniedIpRanges"
                        placeholder="198.51.100.0/24,203.0.113.0/24"
                        className="resize-none"
                        {...form.register('ipRestrictions.deniedIpRanges')} 
                      />
                      <p className="text-xs text-gray-500">Enter IP addresses or CIDR ranges to block, separated by commas</p>
                    </div>
                  )}
                  
                  <div className="flex items-center space-x-2 ml-6">
                    <Checkbox
                      id="notifyOnUnauthorizedAccess"
                      checked={form.watch('ipRestrictions.notifyOnUnauthorizedAccess')}
                      onCheckedChange={(checked) => 
                        form.setValue('ipRestrictions.notifyOnUnauthorizedAccess', Boolean(checked))
                      }
                    />
                    <FormLabel htmlFor="notifyOnUnauthorizedAccess" className="cursor-pointer">
                      Notify on unauthorized access attempts
                    </FormLabel>
                  </div>
                </>
              )}
            </TabsContent>
            
            {/* Security Alerts Tab */}
            <TabsContent value="securityAlerts" className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="alertOnFailedLogins"
                    checked={form.watch('securityAlerts.alertOnFailedLogins')}
                    onCheckedChange={(checked) => 
                      form.setValue('securityAlerts.alertOnFailedLogins', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="alertOnFailedLogins" className="cursor-pointer">
                    Alert on failed login attempts
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="alertOnPasswordChanges"
                    checked={form.watch('securityAlerts.alertOnPasswordChanges')}
                    onCheckedChange={(checked) => 
                      form.setValue('securityAlerts.alertOnPasswordChanges', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="alertOnPasswordChanges" className="cursor-pointer">
                    Alert on password changes
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="alertOnRoleChanges"
                    checked={form.watch('securityAlerts.alertOnRoleChanges')}
                    onCheckedChange={(checked) => 
                      form.setValue('securityAlerts.alertOnRoleChanges', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="alertOnRoleChanges" className="cursor-pointer">
                    Alert on role changes
                  </FormLabel>
                </div>
                
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="alertOnNewUserCreation"
                    checked={form.watch('securityAlerts.alertOnNewUserCreation')}
                    onCheckedChange={(checked) => 
                      form.setValue('securityAlerts.alertOnNewUserCreation', Boolean(checked))
                    }
                  />
                  <FormLabel htmlFor="alertOnNewUserCreation" className="cursor-pointer">
                    Alert on new user creation
                  </FormLabel>
                </div>
              </div>
              
              <div className="space-y-2 pt-2">
                <FormLabel htmlFor="alertDestinationEmail">Alert Destination Email</FormLabel>
                <Input 
                  id="alertDestinationEmail"
                  type="email"
                  placeholder="security@example.com"
                  {...form.register('securityAlerts.alertDestinationEmail')} 
                />
                {form.formState.errors.securityAlerts?.alertDestinationEmail && (
                  <p className="text-sm text-red-500">{form.formState.errors.securityAlerts.alertDestinationEmail.message}</p>
                )}
                <p className="text-xs text-gray-500">Leave empty to use tenant admin email</p>
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
                {isSubmitting ? 'Saving...' : 'Save Security Settings'}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default TenantSecuritySettings; 