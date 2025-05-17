import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { apiRequest } from '../../store/middleware/apiMiddlewareHelper';
import type { RootState } from '../../store/configureStore';
import { useToast } from '../ui/use-toast';
import { 
  Tabs, 
  TabsContent, 
  TabsList, 
  TabsTrigger,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Badge,
  Progress
} from '../ui';
import { RefreshCw, Database, Activity, BarChart3, History } from 'lucide-react';
import IngestionJobsList from './IngestionJobsList';

const IngestionMonitoringDashboard: React.FC = () => {
  const dispatch = useDispatch();
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('overview');
  
  // Get data from Redux store
  const dataSources = useSelector((state: RootState) => state.data.dataSources) || [];
  const ingestionJobs = useSelector((state: RootState) => state.data.ingestionJobs) || [];
  
  // Fetch data on component mount
  useEffect(() => {
    fetchDataSources();
    fetchIngestionJobs();
    
    // Set up polling for active jobs
    const intervalId = window.setInterval(() => {
      const hasActiveJobs = ingestionJobs.some(
        job => job.status === 'running' || job.status === 'queued'
      );
      
      if (hasActiveJobs) {
        fetchIngestionJobs();
      }
    }, 10000); // Poll every 10 seconds for active jobs
    
    return () => window.clearInterval(intervalId);
  }, []);
  
  const fetchDataSources = async () => {
    setIsLoading(true);
    try {
      await dispatch(apiRequest(
        '/api/v1/DataSource',
        'get',
        undefined,
        'FETCH_DATA_SOURCES_SUCCESS',
        'FETCH_DATA_SOURCES_FAILURE'
      ));
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to fetch data sources',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };
  
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
  
  const refreshData = () => {
    fetchDataSources();
    fetchIngestionJobs();
  };
  
  const handleStartIngestion = async (dataSourceId: string) => {
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
      
      fetchIngestionJobs();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to start ingestion job',
        variant: 'destructive',
      });
    }
  };
  
  // Calculate dashboard metrics
  const activeJobs = ingestionJobs.filter(job => job.status === 'running' || job.status === 'queued').length;
  const completedJobs = ingestionJobs.filter(job => job.status === 'completed').length;
  const failedJobs = ingestionJobs.filter(job => job.status === 'failed').length;
  
  // Get total records processed across all jobs
  const totalRecordsProcessed = ingestionJobs.reduce((sum, job) => sum + job.recordsProcessed, 0);
  
  // Get latest job for each data source
  const getLatestJobForDataSource = (dataSourceId: string) => {
    const sourceJobs = ingestionJobs.filter(job => job.dataSourceId === dataSourceId);
    if (sourceJobs.length === 0) return null;
    
    // Sort by start time descending
    return sourceJobs.sort((a, b) => 
      new Date(b.startTime).getTime() - new Date(a.startTime).getTime()
    )[0];
  };
  
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Ingestion Monitoring Dashboard</h1>
          <p className="text-muted-foreground">
            Monitor and manage data ingestion processes across all sources
          </p>
        </div>
        <Button 
          variant="outline" 
          onClick={refreshData}
          disabled={isLoading}
        >
          <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>
      
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Active Jobs
            </CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeJobs}</div>
            <p className="text-xs text-muted-foreground">
              Jobs currently running or queued
            </p>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Completed Jobs
            </CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{completedJobs}</div>
            <p className="text-xs text-muted-foreground">
              Successfully completed ingestion jobs
            </p>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Failed Jobs
            </CardTitle>
            <Database className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{failedJobs}</div>
            <p className="text-xs text-muted-foreground">
              Failed ingestion jobs requiring attention
            </p>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Records Processed
            </CardTitle>
            <History className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalRecordsProcessed.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              Total records processed across all jobs
            </p>
          </CardContent>
        </Card>
      </div>
      
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="overview">Data Sources Overview</TabsTrigger>
          <TabsTrigger value="jobs">All Ingestion Jobs</TabsTrigger>
        </TabsList>
        
        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {dataSources.map(source => {
              const latestJob = getLatestJobForDataSource(source.id);
              return (
                <Card key={source.id}>
                  <CardHeader>
                    <CardTitle>{source.name}</CardTitle>
                    <CardDescription>
                      Type: {source.sourceType}
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-2">
                    <div className="space-y-1">
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Last Refreshed:</span>
                        <span>
                          {latestJob?.endTime ? new Date(latestJob.endTime).toLocaleString() : 'Never'}
                        </span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Status:</span>
                        <span>
                          {latestJob ? (
                            <Badge 
                              variant={
                                latestJob.status === 'completed' ? 'success' : 
                                latestJob.status === 'failed' ? 'destructive' :
                                latestJob.status === 'running' ? 'default' : 'secondary'
                              }
                            >
                              {latestJob.status.charAt(0).toUpperCase() + latestJob.status.slice(1)}
                            </Badge>
                          ) : (
                            <Badge variant="outline">Never Run</Badge>
                          )}
                        </span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Schedule:</span>
                        <span>{source.refreshScheduleType} ({source.refreshInterval})</span>
                      </div>
                    </div>
                    
                    {latestJob && latestJob.status === 'running' && (
                      <div className="space-y-1">
                        <Progress value={latestJob.progress} className="h-2" />
                        <div className="flex justify-between text-xs text-muted-foreground">
                          <span>{latestJob.recordsProcessed} records processed</span>
                          <span>{latestJob.progress.toFixed(1)}%</span>
                        </div>
                      </div>
                    )}
                  </CardContent>
                  <CardFooter>
                    <Button 
                      className="w-full" 
                      size="sm" 
                      disabled={latestJob?.status === 'running' || latestJob?.status === 'queued'}
                      onClick={() => handleStartIngestion(source.id)}
                    >
                      {latestJob?.status === 'running' ? 'Running...' : 
                       latestJob?.status === 'queued' ? 'Queued...' : 'Start Ingestion'}
                    </Button>
                  </CardFooter>
                </Card>
              );
            })}
          </div>
        </TabsContent>
        
        <TabsContent value="jobs">
          <IngestionJobsList />
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default IngestionMonitoringDashboard; 