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
import { Shield, Key, Lock, Eye, AlertTriangle } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { DATA_ACTIONS } from '../../store/slices/dataSlice';

interface TenantSecuritySettingsProps {
  tenantId: string;
  onClose: () => void;
}

interface SecuritySettings {
  id: string;
  passwordMinLength: number;
  passwordRequireUppercase: boolean;
  passwordRequireNumbers: boolean;
  passwordRequireSpecialChars: boolean;
  passwordExpirationDays: number;
  mfaEnabled: boolean;
  mfaRequired: boolean;
  sessionTimeoutMinutes: number;
  maxLoginAttempts: number;
  ipRestrictions: string[];
}

const TenantSecuritySettings: React.FC<TenantSecuritySettingsProps> = ({ tenantId, onClose }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [activeTab, setActiveTab] = useState('password');
  const [securitySettings, setSecuritySettings] = useState<SecuritySettings>({
    id: tenantId,
    passwordMinLength: 8,
    passwordRequireUppercase: true,
    passwordRequireNumbers: true,
    passwordRequireSpecialChars: false,
    passwordExpirationDays: 90,
    mfaEnabled: true,
    mfaRequired: false,
    sessionTimeoutMinutes: 30,
    maxLoginAttempts: 5,
    ipRestrictions: []
  });
  
  const [newIpRestriction, setNewIpRestriction] = useState('');

  // Get tenant from Redux store
  const tenant = useSelector((state: RootState) => 
    state.data.tenants.find(t => t.id === tenantId)
  );

  // Fetch security settings on mount
  useEffect(() => {
    fetchSecuritySettings();
  }, [tenantId]);

  const fetchSecuritySettings = async () => {
    setIsLoading(true);
    try {
      // Placeholder for actual API call - would be implemented in real app
      // const response = await dispatch(apiRequest(
      //   `/api/v1/Tenants/${tenantId}/security`,
      //   'get',
      //   undefined,
      //   'FETCH_TENANT_SECURITY_SUCCESS',
      //   'FETCH_TENANT_SECURITY_FAILURE'
      // ));
      
      // For demo purposes, just use default values + tenant ID
      setSecuritySettings(prev => ({
        ...prev,
        id: tenantId
      }));

      // Simulate an API delay
      setTimeout(() => {
        setIsLoading(false);
      }, 500);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch security settings',
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
      //   `/api/v1/Tenants/${tenantId}/security`,
      //   'put',
      //   securitySettings,
      //   'UPDATE_TENANT_SECURITY_SUCCESS',
      //   'UPDATE_TENANT_SECURITY_FAILURE'
      // ));
      
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 800));
      
      toast({
        title: 'Success',
        description: 'Security settings updated successfully',
      });
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to update security settings',
        variant: 'destructive',
      });
    } finally {
      setIsSaving(false);
    }
  };

  const handleInputChange = (field: keyof SecuritySettings, value: any) => {
    setSecuritySettings(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleAddIpRestriction = () => {
    if (!newIpRestriction) return;
    
    // Simple validation for IP address or CIDR notation
    const ipPattern = /^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(\/\d{1,2})?$/;
    if (!ipPattern.test(newIpRestriction)) {
      toast({
        title: 'Invalid IP',
        description: 'Please enter a valid IP address or CIDR notation',
        variant: 'destructive',
      });
      return;
    }
    
    // Add to restrictions list
    setSecuritySettings(prev => ({
      ...prev,
      ipRestrictions: [...prev.ipRestrictions, newIpRestriction]
    }));
    
    // Clear input
    setNewIpRestriction('');
  };

  const handleRemoveIpRestriction = (ip: string) => {
    setSecuritySettings(prev => ({
      ...prev,
      ipRestrictions: prev.ipRestrictions.filter(item => item !== ip)
    }));
  };

  const calculatePasswordStrength = () => {
    let strength = 0;
    if (securitySettings.passwordMinLength >= 8) strength += 1;
    if (securitySettings.passwordMinLength >= 12) strength += 1;
    if (securitySettings.passwordRequireUppercase) strength += 1;
    if (securitySettings.passwordRequireNumbers) strength += 1;
    if (securitySettings.passwordRequireSpecialChars) strength += 1;
    
    if (strength <= 1) return 'Weak';
    if (strength <= 3) return 'Moderate';
    return 'Strong';
  };

  const getPasswordStrengthColor = () => {
    const strength = calculatePasswordStrength();
    if (strength === 'Weak') return 'bg-red-500';
    if (strength === 'Moderate') return 'bg-yellow-500';
    return 'bg-green-500';
  };

  return (
    <Dialog open={true} onOpenChange={() => onClose()}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            <span>Security Settings: {tenant?.name || 'Loading...'}</span>
          </DialogTitle>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center items-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
          </div>
        ) : (
          <>
            <Tabs defaultValue="password" value={activeTab} onValueChange={setActiveTab}>
              <TabsList className="grid grid-cols-3 mb-6">
                <TabsTrigger value="password" className="flex items-center gap-2">
                  <Lock className="h-4 w-4" />
                  <span>Password</span>
                </TabsTrigger>
                <TabsTrigger value="auth" className="flex items-center gap-2">
                  <Key className="h-4 w-4" />
                  <span>Authentication</span>
                </TabsTrigger>
                <TabsTrigger value="access" className="flex items-center gap-2">
                  <Eye className="h-4 w-4" />
                  <span>Access Control</span>
                </TabsTrigger>
              </TabsList>

              <TabsContent value="password" className="space-y-4">
                <div>
                  <h3 className="font-medium mb-2">Password Strength</h3>
                  <div className="h-2 w-full bg-gray-200 rounded-full">
                    <div 
                      className={`h-2 rounded-full ${getPasswordStrengthColor()}`} 
                      style={{ width: `${(calculatePasswordStrength() === 'Weak' ? 33 : calculatePasswordStrength() === 'Moderate' ? 66 : 100)}%` }}
                    ></div>
                  </div>
                  <p className="text-sm mt-1">
                    Current policy enforces <span className="font-medium">{calculatePasswordStrength()}</span> passwords
                  </p>
                </div>

                <FormItem>
                  <FormLabel>Minimum Password Length</FormLabel>
                  <div className="flex items-center gap-4">
                    <Input 
                      type="number"
                      min={6}
                      max={24}
                      step={1}
                      value={securitySettings.passwordMinLength}
                      onChange={(e) => handleInputChange('passwordMinLength', parseInt(e.target.value, 10))}
                      className="flex-1"
                    />
                    <div className="w-12 text-center font-medium">
                      {securitySettings.passwordMinLength}
                    </div>
                  </div>
                  <FormDescription>
                    Minimum number of characters required in passwords
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Password Complexity</FormLabel>
                  <div className="space-y-2 mt-2">
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id="require-uppercase"
                        checked={securitySettings.passwordRequireUppercase}
                        onCheckedChange={(checked) => 
                          handleInputChange('passwordRequireUppercase', Boolean(checked))
                        }
                      />
                      <FormLabel htmlFor="require-uppercase" className="cursor-pointer font-normal">
                        Require uppercase letters
                      </FormLabel>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id="require-numbers"
                        checked={securitySettings.passwordRequireNumbers}
                        onCheckedChange={(checked) => 
                          handleInputChange('passwordRequireNumbers', Boolean(checked))
                        }
                      />
                      <FormLabel htmlFor="require-numbers" className="cursor-pointer font-normal">
                        Require numbers
                      </FormLabel>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id="require-special"
                        checked={securitySettings.passwordRequireSpecialChars}
                        onCheckedChange={(checked) => 
                          handleInputChange('passwordRequireSpecialChars', Boolean(checked))
                        }
                      />
                      <FormLabel htmlFor="require-special" className="cursor-pointer font-normal">
                        Require special characters
                      </FormLabel>
                    </div>
                  </div>
                </FormItem>

                <FormItem>
                  <FormLabel>Password Expiration (Days)</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={0}
                      max={365}
                      value={securitySettings.passwordExpirationDays}
                      onChange={(e) => handleInputChange('passwordExpirationDays', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    Number of days before passwords expire (0 means never expire)
                  </FormDescription>
                </FormItem>
              </TabsContent>

              <TabsContent value="auth" className="space-y-4">
                <FormItem>
                  <div className="flex items-center justify-between">
                    <FormLabel>Multi-Factor Authentication</FormLabel>
                    <div className="space-x-2">
                      <Button 
                        variant="outline" 
                        size="sm"
                        disabled={!securitySettings.mfaEnabled}
                        onClick={() => handleInputChange('mfaRequired', !securitySettings.mfaRequired)}
                      >
                        {securitySettings.mfaRequired ? 'Make Optional' : 'Make Required'}
                      </Button>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2 mt-2">
                    <Checkbox
                      id="mfa-enabled"
                      checked={securitySettings.mfaEnabled}
                      onCheckedChange={(checked) => {
                        const isEnabled = Boolean(checked);
                        handleInputChange('mfaEnabled', isEnabled);
                        if (!isEnabled) {
                          handleInputChange('mfaRequired', false);
                        }
                      }}
                    />
                    <FormLabel htmlFor="mfa-enabled" className="cursor-pointer font-normal">
                      Enable multi-factor authentication
                    </FormLabel>
                  </div>
                  <FormDescription>
                    {securitySettings.mfaEnabled 
                      ? securitySettings.mfaRequired 
                        ? 'MFA is enabled and required for all users' 
                        : 'MFA is enabled but optional for users'
                      : 'MFA is disabled for this tenant'
                    }
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Session Timeout (Minutes)</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={5}
                      max={240}
                      value={securitySettings.sessionTimeoutMinutes}
                      onChange={(e) => handleInputChange('sessionTimeoutMinutes', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    How long before inactive user sessions are terminated
                  </FormDescription>
                </FormItem>

                <FormItem>
                  <FormLabel>Maximum Login Attempts</FormLabel>
                  <FormControl>
                    <Input 
                      type="number"
                      min={1}
                      max={10}
                      value={securitySettings.maxLoginAttempts}
                      onChange={(e) => handleInputChange('maxLoginAttempts', parseInt(e.target.value, 10))}
                    />
                  </FormControl>
                  <FormDescription>
                    Number of failed login attempts allowed before account lockout
                  </FormDescription>
                </FormItem>
              </TabsContent>

              <TabsContent value="access" className="space-y-4">
                <FormItem>
                  <FormLabel>IP Address Restrictions</FormLabel>
                  <div className="flex space-x-2">
                    <Input
                      value={newIpRestriction}
                      onChange={(e) => setNewIpRestriction(e.target.value)}
                      placeholder="Enter IP address or CIDR range"
                    />
                    <Button type="button" onClick={handleAddIpRestriction}>
                      Add
                    </Button>
                  </div>
                  <FormDescription>
                    Restrict access to specific IP addresses or ranges (e.g., 192.168.1.1 or 10.0.0.0/24)
                  </FormDescription>
                </FormItem>

                {securitySettings.ipRestrictions.length > 0 ? (
                  <div className="border rounded-md overflow-hidden">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="px-4 py-2 text-left">IP Address/Range</th>
                          <th className="px-4 py-2 text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {securitySettings.ipRestrictions.map((ip, index) => (
                          <tr key={index} className="border-b">
                            <td className="px-4 py-2">{ip}</td>
                            <td className="px-4 py-2 text-right">
                              <Button 
                                variant="ghost" 
                                size="sm" 
                                onClick={() => handleRemoveIpRestriction(ip)}
                              >
                                Remove
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-amber-600 bg-amber-50 p-3 rounded-md">
                    <AlertTriangle className="h-5 w-5" />
                    <p className="text-sm">No IP restrictions are currently configured. All IP addresses are allowed.</p>
                  </div>
                )}
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
                {isSaving ? 'Saving...' : 'Save Security Settings'}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default TenantSecuritySettings; 