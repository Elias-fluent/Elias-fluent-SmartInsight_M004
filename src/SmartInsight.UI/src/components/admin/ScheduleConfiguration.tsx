import React from 'react';
import type { UseFormReturn } from 'react-hook-form';
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '../ui/form';
import { Input } from '../ui/input';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '../ui/select';
import { Card, CardContent } from '../../components/ui/card';
import { HelpCircle } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../../components/ui/tooltip';

// Extends FormValues from DataSourceForm
interface ScheduleConfigurationProps {
  form: UseFormReturn<any>;
}

const ScheduleConfiguration: React.FC<ScheduleConfigurationProps> = ({ form }) => {
  const scheduleType = form.watch('refreshScheduleType');
  
  // Handle refresh type change
  const handleRefreshTypeChange = (value: string) => {
    form.setValue('refreshScheduleType', value);
    
    // Set default intervals based on schedule type
    switch (value) {
      case 'minutes':
        form.setValue('refreshInterval', 30);
        break;
      case 'hourly':
        form.setValue('refreshInterval', 1);
        break;
      case 'daily':
        form.setValue('refreshInterval', 1);
        break;
      case 'weekly':
        form.setValue('refreshInterval', 1);
        break;
      case 'monthly':
        form.setValue('refreshInterval', 1);
        break;
    }
  };
  
  return (
    <div className="space-y-4">
      <div className="mb-4">
        <h3 className="text-sm font-medium mb-1">Ingestion Schedule</h3>
        <p className="text-xs text-muted-foreground">
          Define how often data should be refreshed from this source.
        </p>
      </div>
      
      <FormField
        control={form.control}
        name="refreshScheduleType"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Refresh Type</FormLabel>
            <Select 
              onValueChange={handleRefreshTypeChange} 
              defaultValue={field.value}
            >
              <FormControl>
                <SelectTrigger>
                  <SelectValue placeholder="Select a schedule type" />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                <SelectItem value="minutes">Every X Minutes</SelectItem>
                <SelectItem value="hourly">Hourly</SelectItem>
                <SelectItem value="daily">Daily</SelectItem>
                <SelectItem value="weekly">Weekly</SelectItem>
                <SelectItem value="monthly">Monthly</SelectItem>
                <SelectItem value="custom">Custom (Cron)</SelectItem>
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />
      
      {scheduleType !== 'custom' && (
        <FormField
          control={form.control}
          name="refreshInterval"
          render={({ field }) => (
            <FormItem>
              <FormLabel>
                {scheduleType === 'minutes' ? 'Minutes between refreshes' :
                  scheduleType === 'hourly' ? 'Hours between refreshes' :
                  scheduleType === 'daily' ? 'Days between refreshes' :
                  scheduleType === 'weekly' ? 'Day of week (0-6, Sunday=0)' :
                  scheduleType === 'monthly' ? 'Day of month (1-31)' : 'Interval'}
              </FormLabel>
              <FormControl>
                <Input 
                  type="number" 
                  min={1} 
                  {...field}
                  onChange={(e) => field.onChange(Number(e.target.value))}
                  value={field.value || (scheduleType === 'minutes' ? 30 : 1)}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      )}
      
      {scheduleType === 'custom' && (
        <FormField
          control={form.control}
          name="refreshCronExpression"
          render={({ field }) => (
            <FormItem>
              <div className="flex items-center space-x-1">
                <FormLabel>Cron Expression</FormLabel>
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <span>
                        <HelpCircle className="w-3 h-3 text-muted-foreground" />
                      </span>
                    </TooltipTrigger>
                    <TooltipContent className="max-w-xs">
                      <p className="text-xs">
                        Use cron syntax: <code>minute hour day-of-month month day-of-week</code>
                        <br />
                        Example: <code>0 0 * * *</code> (daily at midnight)
                      </p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
              <FormControl>
                <Input 
                  placeholder="0 0 * * *" 
                  {...field}
                  value={field.value || ''}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      )}
      
      <Card className="border-dashed">
        <CardContent className="pt-4">
          <h4 className="text-sm font-medium mb-2">Preview</h4>
          <p className="text-sm">
            {scheduleType === 'minutes' && form.watch('refreshInterval')
              ? `Every ${form.watch('refreshInterval')} minute(s)`
              : scheduleType === 'hourly' && form.watch('refreshInterval')
              ? `Every ${form.watch('refreshInterval')} hour(s)`
              : scheduleType === 'daily' && form.watch('refreshInterval')
              ? `Every ${form.watch('refreshInterval')} day(s)`
              : scheduleType === 'weekly' && form.watch('refreshInterval') !== undefined
              ? `Weekly on ${getWeekdayName(form.watch('refreshInterval') % 7)}`
              : scheduleType === 'monthly' && form.watch('refreshInterval')
              ? `Monthly on day ${form.watch('refreshInterval')}`
              : scheduleType === 'custom' && form.watch('refreshCronExpression')
              ? `Custom schedule: ${form.watch('refreshCronExpression')}`
              : 'Schedule not set'
            }
          </p>
        </CardContent>
      </Card>
    </div>
  );
};

// Helper function to convert day number to name
const getWeekdayName = (day: number): string => {
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  return days[day] || 'Invalid day';
};

export default ScheduleConfiguration; 