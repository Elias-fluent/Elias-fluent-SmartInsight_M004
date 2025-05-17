import React, { useEffect, useState } from 'react';
import { 
  Button,
  Table, 
  TableBody, 
  TableCell, 
  TableHead, 
  TableHeader, 
  TableRow,
  Badge,
  Progress,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from '../ui';
import { useToast } from '../ui/use-toast';
import { 
  RefreshCw, 
  Eye, 
  Play, 
  Square, 
  Clock, 
  CheckCircle2, 
  XCircle, 
  Hourglass
} from 'lucide-react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/configureStore';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import IngestionJobDetail from './IngestionJobDetail.tsx';
import type { IngestionJob } from '../../store/slices/dataSlice';

// Define component props interface
interface IngestionJobsListProps {
  className?: string;
}

const IngestionJobsList: React.FC<IngestionJobsListProps> = ({ className }) => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [showJobDetail, setShowJobDetail] = useState(false);
  const [selectedJob, setSelectedJob] = useState<IngestionJob | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('all');

  // Get ingestion jobs from Redux store
  const ingestionJobs = useSelector((state: RootState) => state.data.ingestionJobs) || [];
  const dataSources = useSelector((state: RootState) => state.data.dataSources) || [];

  // Filter jobs based on selected status
  const filteredJobs = statusFilter === 'all' 
    ? ingestionJobs 
    : ingestionJobs.filter(job => job.status === statusFilter);

  // Fetch ingestion jobs on component mount and refresh
  useEffect(() => {
    fetchIngestionJobs();
  }, []);

  const fetchIngestionJobs = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/IngestionJobs',
        'get',
        undefined,
        'FETCH_INGESTION_JOBS_SUCCESS',
        'FETCH_INGESTION_JOBS_FAILURE'
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch ingestion jobs',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleViewDetail = (job: IngestionJob) => {
    setSelectedJob(job);
    setShowJobDetail(true);
  };

  const handleStartJob = async (dataSourceId: string) => {
    try {
      await dispatch(apiRequest(
        `/api/v1/DataSource/${dataSourceId}/startIngestion`,
        'post',
        undefined,
        'START_INGESTION_JOB_SUCCESS',
        'START_INGESTION_JOB_FAILURE'
      ));
      
      toast({
        title: 'Success',
        description: 'Ingestion job started successfully',
      });
      
      // Refresh job list
      fetchIngestionJobs();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to start ingestion job',
        variant: 'destructive',
      });
    }
  };

  const handleCancelJob = async (jobId: string) => {
    try {
      await dispatch(apiRequest(
        `/api/v1/IngestionJobs/${jobId}/cancel`,
        'post',
        undefined,
        'CANCEL_INGESTION_JOB_SUCCESS',
        'CANCEL_INGESTION_JOB_FAILURE'
      ));
      
      toast({
        title: 'Success',
        description: 'Ingestion job cancelled successfully',
      });
      
      // Refresh job list
      fetchIngestionJobs();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to cancel ingestion job',
        variant: 'destructive',
      });
    }
  };

  // Get status badge based on job status
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
      case 'paused':
        return <Badge variant="outline" className="flex items-center gap-1">
          <Clock className="h-3 w-3" />
          <span>Paused</span>
        </Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  // Format date-time for display
  const formatDateTime = (dateTimeString: string) => {
    if (!dateTimeString) return 'N/A';
    return new Date(dateTimeString).toLocaleString();
  };

  // Get data source name from its ID
  const getDataSourceName = (dataSourceId: string) => {
    const dataSource = dataSources.find(ds => ds.id === dataSourceId);
    return dataSource ? dataSource.name : 'Unknown Source';
  };

  return (
    <div className={className}>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-xl font-bold">Ingestion Jobs</h2>
          <p className="text-muted-foreground">
            Monitor and manage data ingestion processes
          </p>
        </div>
        <div className="flex gap-2">
          <Select
            value={statusFilter}
            onValueChange={setStatusFilter}
          >
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Filter by status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Statuses</SelectItem>
              <SelectItem value="queued">Queued</SelectItem>
              <SelectItem value="running">Running</SelectItem>
              <SelectItem value="completed">Completed</SelectItem>
              <SelectItem value="failed">Failed</SelectItem>
              <SelectItem value="cancelled">Cancelled</SelectItem>
              <SelectItem value="paused">Paused</SelectItem>
            </SelectContent>
          </Select>
          <Button 
            variant="outline" 
            size="sm" 
            onClick={fetchIngestionJobs}
            disabled={isLoading}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>
      ) : filteredJobs.length === 0 ? (
        <div className="text-center py-8 border rounded-md bg-background">
          <p className="text-muted-foreground mb-4">No ingestion jobs found</p>
          {statusFilter !== 'all' && (
            <Button onClick={() => setStatusFilter('all')} variant="outline">
              Clear Filter
            </Button>
          )}
        </div>
      ) : (
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Data Source</TableHead>
                <TableHead>Start Time</TableHead>
                <TableHead>End Time</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Progress</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredJobs.map((job) => (
                <TableRow key={job.id}>
                  <TableCell className="font-mono text-xs">{job.id.substring(0, 8)}...</TableCell>
                  <TableCell>{job.dataSourceName || getDataSourceName(job.dataSourceId)}</TableCell>
                  <TableCell>{formatDateTime(job.startTime)}</TableCell>
                  <TableCell>{job.endTime ? formatDateTime(job.endTime) : 'N/A'}</TableCell>
                  <TableCell>{getStatusBadge(job.status)}</TableCell>
                  <TableCell>
                    <div className="flex flex-col space-y-1">
                      <Progress value={job.progress} className="h-2" />
                      <span className="text-xs text-muted-foreground">
                        {job.recordsProcessed} 
                        {job.totalRecords ? ` / ${job.totalRecords}` : ''}
                        {' records processed'}
                      </span>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleViewDetail(job)}
                        title="View Details"
                      >
                        <Eye className="h-4 w-4" />
                        <span className="sr-only">View Details</span>
                      </Button>
                      {job.status === 'running' && (
                        <Button 
                          variant="ghost" 
                          size="icon" 
                          onClick={() => handleCancelJob(job.id)}
                          title="Cancel Job"
                        >
                          <Square className="h-4 w-4" />
                          <span className="sr-only">Cancel</span>
                        </Button>
                      )}
                      {job.status !== 'running' && job.status !== 'queued' && (
                        <Button 
                          variant="ghost" 
                          size="icon" 
                          onClick={() => handleStartJob(job.dataSourceId)}
                          title="Start New Job"
                        >
                          <Play className="h-4 w-4" />
                          <span className="sr-only">Start New</span>
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Ingestion Job Detail Dialog */}
      {showJobDetail && selectedJob && (
        <IngestionJobDetail
          jobId={selectedJob.id}
          onClose={() => setShowJobDetail(false)}
          onRefresh={fetchIngestionJobs}
        />
      )}
    </div>
  );
};

export default IngestionJobsList; 