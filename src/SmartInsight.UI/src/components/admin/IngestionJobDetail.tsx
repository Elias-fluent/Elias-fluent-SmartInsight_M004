import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import { useToast } from '../ui/use-toast';
import type { RootState } from '../../store/configureStore';
import type { IngestionJob } from '../../store/slices/dataSlice';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Progress,
  Button,
  Badge,
  ScrollArea,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '../ui';
import {
  Play,
  Square,
  RefreshCw,
  AlignLeft,
  BarChart3,
  Info,
  AlertTriangle,
  AlertCircle,
  Hourglass,
  CheckCircle2,
  XCircle,
} from 'lucide-react';

interface IngestionJobDetailProps {
  jobId: string;
  onClose: () => void;
  onRefresh?: () => void;
}

const IngestionJobDetail: React.FC<IngestionJobDetailProps> = ({
  jobId,
  onClose,
  onRefresh,
}) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('summary');
  
  // Get job details from store
  const selectedJob = useSelector((state: RootState) => 
    state.data.ingestionJobs.find(job => job.id === jobId)
  );
  
  const dataSources = useSelector((state: RootState) => state.data.dataSources || []);
  
  // Fetch job details on mount
  useEffect(() => {
    fetchJobDetails();
    
    // Set up polling for in-progress jobs
    let intervalId: number;
    if (selectedJob && (selectedJob.status === 'running' || selectedJob.status === 'queued')) {
      intervalId = window.setInterval(fetchJobDetails, 5000); // Poll every 5 seconds
    }
    
    return () => {
      if (intervalId) window.clearInterval(intervalId);
    };
  }, [jobId, selectedJob?.status]);
  
  const fetchJobDetails = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        `/api/v1/IngestionJobs/${jobId}`,
        'get',
        undefined,
        'FETCH_INGESTION_JOB_DETAILS_SUCCESS',
        'FETCH_INGESTION_JOB_DETAILS_FAILURE'
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch job details',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };
  
  const handleCancelJob = async () => {
    if (!selectedJob) return;
    
    try {
      await dispatch(apiRequest(
        `/api/v1/IngestionJobs/${selectedJob.id}/cancel`,
        'post',
        undefined,
        'CANCEL_INGESTION_JOB_SUCCESS',
        'CANCEL_INGESTION_JOB_FAILURE'
      ));
      
      toast({
        title: 'Success',
        description: 'Ingestion job cancelled successfully',
      });
      
      if (onRefresh) onRefresh();
      fetchJobDetails();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to cancel ingestion job',
        variant: 'destructive',
      });
    }
  };
  
  const handleStartNewJob = async () => {
    if (!selectedJob) return;
    
    try {
      await dispatch(apiRequest(
        `/api/v1/DataSource/${selectedJob.dataSourceId}/startIngestion`,
        'post',
        undefined,
        'START_INGESTION_JOB_SUCCESS',
        'START_INGESTION_JOB_FAILURE'
      ));
      
      toast({
        title: 'Success',
        description: 'New ingestion job started successfully',
      });
      
      if (onRefresh) onRefresh();
      onClose();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to start new ingestion job',
        variant: 'destructive',
      });
    }
  };
  
  // Get data source name
  const getDataSourceName = (dataSourceId: string) => {
    const dataSource = dataSources.find(ds => ds.id === dataSourceId);
    return dataSource ? dataSource.name : 'Unknown Source';
  };
  
  // Format date-time
  const formatDateTime = (dateTimeString?: string) => {
    if (!dateTimeString) return 'N/A';
    return new Date(dateTimeString).toLocaleString();
  };
  
  // Format duration
  const formatDuration = (startTime?: string, endTime?: string) => {
    if (!startTime) return 'N/A';
    const start = new Date(startTime).getTime();
    const end = endTime ? new Date(endTime).getTime() : Date.now();
    
    const durationMs = end - start;
    const seconds = Math.floor(durationMs / 1000) % 60;
    const minutes = Math.floor(durationMs / (1000 * 60)) % 60;
    const hours = Math.floor(durationMs / (1000 * 60 * 60));
    
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  };
  
  // Get log entry icon
  const getLogEntryIcon = (level: string) => {
    switch (level) {
      case 'info':
        return <Info className="h-4 w-4 text-blue-500" />;
      case 'warning':
        return <AlertTriangle className="h-4 w-4 text-yellow-500" />;
      case 'error':
        return <AlertCircle className="h-4 w-4 text-red-500" />;
      default:
        return <Info className="h-4 w-4" />;
    }
  };
  
  // Get status badge
  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'queued':
        return <Badge variant="secondary" className="flex items-center gap-1">
          <Hourglass className="h-3 w-3" />
          <span>Queued</span>
        </Badge>;
      case 'running':
        return <Badge variant="default" className="flex items-center gap-1">
          <Play className="h-3 w-3" />
          <span>Running</span>
        </Badge>;
      case 'completed':
        return <Badge variant="success" className="flex items-center gap-1">
          <CheckCircle2 className="h-3 w-3" />
          <span>Completed</span>
        </Badge>;
      case 'failed':
        return <Badge variant="destructive" className="flex items-center gap-1">
          <XCircle className="h-3 w-3" />
          <span>Failed</span>
        </Badge>;
      case 'cancelled':
        return <Badge variant="outline" className="flex items-center gap-1">
          <Square className="h-3 w-3" />
          <span>Cancelled</span>
        </Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };
  
  if (!selectedJob) {
    return (
      <Dialog open={true} onOpenChange={() => onClose()}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Job Not Found</DialogTitle>
          </DialogHeader>
          <p>The requested ingestion job could not be found.</p>
          <DialogFooter>
            <Button onClick={onClose}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }
  
  return (
    <Dialog open={true} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col">
        <DialogHeader>
          <DialogTitle className="text-xl flex items-center gap-2">
            Ingestion Job Details
            {isLoading && <RefreshCw className="h-4 w-4 animate-spin ml-2" />}
          </DialogTitle>
        </DialogHeader>
        
        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
          <TabsList>
            <TabsTrigger value="summary" className="flex items-center gap-1">
              <BarChart3 className="h-4 w-4" />
              <span>Summary</span>
            </TabsTrigger>
            <TabsTrigger value="logs" className="flex items-center gap-1">
              <AlignLeft className="h-4 w-4" />
              <span>Logs</span>
            </TabsTrigger>
          </TabsList>
          
          <TabsContent value="summary" className="flex-1 overflow-hidden flex flex-col">
            <div className="grid grid-cols-2 gap-4 mb-4">
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium">Status</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center gap-2">
                    {getStatusBadge(selectedJob.status)}
                    {selectedJob.status === 'failed' && (
                      <span className="text-sm text-destructive">{selectedJob.errorMessage}</span>
                    )}
                  </div>
                </CardContent>
              </Card>
              
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm font-medium">Data Source</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center gap-2">
                    <span>{selectedJob.dataSourceName || getDataSourceName(selectedJob.dataSourceId)}</span>
                  </div>
                </CardContent>
              </Card>
            </div>
            
            <Card className="mb-4">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium">Progress</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <Progress value={selectedJob.progress} className="h-2" />
                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span>{selectedJob.recordsProcessed} records processed</span>
                    <span>{selectedJob.progress.toFixed(1)}%</span>
                  </div>
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium">Timing Information</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-1">
                  <div className="grid grid-cols-2 text-sm">
                    <span className="text-muted-foreground">Start Time:</span>
                    <span>{formatDateTime(selectedJob.startTime)}</span>
                  </div>
                  <div className="grid grid-cols-2 text-sm">
                    <span className="text-muted-foreground">End Time:</span>
                    <span>{selectedJob.endTime ? formatDateTime(selectedJob.endTime) : 'In Progress'}</span>
                  </div>
                  <div className="grid grid-cols-2 text-sm">
                    <span className="text-muted-foreground">Duration:</span>
                    <span>{formatDuration(selectedJob.startTime, selectedJob.endTime)}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
          
          <TabsContent value="logs" className="flex-1 overflow-hidden">
            <ScrollArea className="h-[350px] rounded-md border">
              {selectedJob.logEntries && selectedJob.logEntries.length > 0 ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[100px]">Time</TableHead>
                      <TableHead className="w-[80px]">Level</TableHead>
                      <TableHead>Message</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {selectedJob.logEntries.map((log) => (
                      <TableRow key={log.id}>
                        <TableCell className="text-xs font-mono">
                          {new Date(log.timestamp).toLocaleTimeString()}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            {getLogEntryIcon(log.level)}
                            <span className="text-xs capitalize">{log.level}</span>
                          </div>
                        </TableCell>
                        <TableCell className="text-sm">
                          {log.message}
                          {log.details && (
                            <div className="text-xs mt-1 text-muted-foreground border-l-2 pl-2 border-muted-foreground/20">
                              {log.details}
                            </div>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <div className="p-4 text-center text-muted-foreground">
                  No log entries available
                </div>
              )}
            </ScrollArea>
          </TabsContent>
        </Tabs>
        
        <DialogFooter className="flex justify-between items-center">
          <div>
            <Button variant="outline" size="sm" onClick={fetchJobDetails} disabled={isLoading}>
              <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
          <div className="flex gap-2">
            {selectedJob.status === 'running' && (
              <Button variant="destructive" size="sm" onClick={handleCancelJob}>
                <Square className="h-4 w-4 mr-2" />
                Cancel Job
              </Button>
            )}
            {selectedJob.status !== 'running' && selectedJob.status !== 'queued' && (
              <Button variant="outline" size="sm" onClick={handleStartNewJob}>
                <Play className="h-4 w-4 mr-2" />
                Start New Job
              </Button>
            )}
            <Button onClick={onClose}>Close</Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default IngestionJobDetail; 