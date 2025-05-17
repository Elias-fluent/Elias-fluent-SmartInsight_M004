import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

// Define the data state type
export interface DataState {
  dataSources: DataSource[];
  users: User[];
  tenants: Tenant[];
  datasets: Dataset[];
  queries: Query[];
  visualizations: Visualization[];
  ingestionJobs: IngestionJob[];
  selectedDataSource: DataSource | null;
  selectedDataset: any | null;
  selectedIngestionJob: IngestionJob | null;
  isLoading: boolean;
  error: string | null;
  lastUpdated: string | null;
}

// Define data source type
export interface DataSource {
  id: string;
  name: string;
  sourceType: string;
  connectionString: string;
  username?: string;
  password?: string;
  apiKey?: string;
  description?: string;
  refreshScheduleType: 'minutes' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'custom';
  refreshInterval: number;
  refreshCronExpression?: string;
  status?: string;
  lastRefreshed?: string;
}

// Define user type
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  tenantId: string;
  tenantName?: string;
  lastLogin?: string;
}

// Define tenant type
export interface Tenant {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
  dataSourceCount?: number;
  userCount?: number;
}

// Define dataset type
export interface Dataset {
  id: string;
  name: string;
  description: string;
  dataSourceId: string;
  schema: DatasetSchema;
  lastUpdated: number;
  rowCount: number;
}

// Define dataset schema type
export interface DatasetSchema {
  columns: DatasetColumn[];
}

// Define dataset column type
export interface DatasetColumn {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'object' | 'array';
  nullable: boolean;
  isPrimaryKey?: boolean;
  isForeignKey?: boolean;
  referencesTable?: string;
  referencesColumn?: string;
}

// Define query type
export interface Query {
  id: string;
  name: string;
  description: string;
  sql: string;
  datasetId: string;
  createdAt: number;
  updatedAt: number;
  lastExecuted: number | null;
  results: any[] | null;
}

// Define visualization type
export interface Visualization {
  id: string;
  name: string;
  description: string;
  type: 'bar' | 'line' | 'pie' | 'scatter' | 'table' | 'custom';
  queryId: string;
  config: any;
  createdAt: number;
  updatedAt: number;
}

// Define ingestion job type
export interface IngestionJob {
  id: string;
  dataSourceId: string;
  dataSourceName?: string;
  startTime: string;
  endTime?: string;
  status: 'queued' | 'running' | 'completed' | 'failed' | 'cancelled' | 'paused';
  progress: number;
  recordsProcessed: number;
  totalRecords?: number;
  errorMessage?: string;
  logEntries?: IngestionLogEntry[];
}

// Define ingestion log entry type
export interface IngestionLogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warning' | 'error';
  message: string;
  details?: string;
}

// Define action types
export const DATA_ACTIONS = {
  FETCH_DATA_SOURCES_REQUEST: 'data/fetchDataSourcesRequest',
  FETCH_DATA_SOURCES_SUCCESS: 'data/fetchDataSourcesSuccess',
  FETCH_DATA_SOURCES_FAILURE: 'data/fetchDataSourcesFailure',
  SET_SELECTED_DATA_SOURCE: 'data/setSelectedDataSource',
  
  FETCH_DATASETS_REQUEST: 'data/fetchDatasetsRequest',
  FETCH_DATASETS_SUCCESS: 'data/fetchDatasetsSuccess',
  FETCH_DATASETS_FAILURE: 'data/fetchDatasetsFailure',
  SET_SELECTED_DATASET: 'data/setSelectedDataset',
  
  FETCH_QUERIES_REQUEST: 'data/fetchQueriesRequest',
  FETCH_QUERIES_SUCCESS: 'data/fetchQueriesSuccess',
  FETCH_QUERIES_FAILURE: 'data/fetchQueriesFailure',
  
  EXECUTE_QUERY_REQUEST: 'data/executeQueryRequest',
  EXECUTE_QUERY_SUCCESS: 'data/executeQuerySuccess',
  EXECUTE_QUERY_FAILURE: 'data/executeQueryFailure',
  
  SAVE_QUERY: 'data/saveQuery',
  DELETE_QUERY: 'data/deleteQuery',
  
  FETCH_VISUALIZATIONS_REQUEST: 'data/fetchVisualizationsRequest',
  FETCH_VISUALIZATIONS_SUCCESS: 'data/fetchVisualizationsSuccess',
  FETCH_VISUALIZATIONS_FAILURE: 'data/fetchVisualizationsFailure',
  
  SAVE_VISUALIZATION: 'data/saveVisualization',
  DELETE_VISUALIZATION: 'data/deleteVisualization',
  
  FETCH_INGESTION_JOBS_REQUEST: 'data/fetchIngestionJobsRequest',
  FETCH_INGESTION_JOBS_SUCCESS: 'data/fetchIngestionJobsSuccess',
  FETCH_INGESTION_JOBS_FAILURE: 'data/fetchIngestionJobsFailure',
  
  FETCH_INGESTION_JOB_DETAILS_REQUEST: 'data/fetchIngestionJobDetailsRequest',
  FETCH_INGESTION_JOB_DETAILS_SUCCESS: 'data/fetchIngestionJobDetailsSuccess',
  FETCH_INGESTION_JOB_DETAILS_FAILURE: 'data/fetchIngestionJobDetailsFailure',
  
  SET_SELECTED_INGESTION_JOB: 'data/setSelectedIngestionJob',
  
  START_INGESTION_JOB_REQUEST: 'data/startIngestionJobRequest',
  START_INGESTION_JOB_SUCCESS: 'data/startIngestionJobSuccess',
  START_INGESTION_JOB_FAILURE: 'data/startIngestionJobFailure',
  
  CANCEL_INGESTION_JOB_REQUEST: 'data/cancelIngestionJobRequest',
  CANCEL_INGESTION_JOB_SUCCESS: 'data/cancelIngestionJobSuccess',
  CANCEL_INGESTION_JOB_FAILURE: 'data/cancelIngestionJobFailure',
  
  CLEAR_ERROR: 'data/clearError',
} as const;

// Define action interfaces
export type DataAction =
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_SUCCESS; payload: DataSource[] }
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_FAILURE; payload: string }
  | { type: typeof DATA_ACTIONS.SET_SELECTED_DATA_SOURCE; payload: DataSource | null }
  
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_SUCCESS; payload: Dataset[] }
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_FAILURE; payload: string }
  | { type: typeof DATA_ACTIONS.SET_SELECTED_DATASET; payload: any | null }
  
  | { type: typeof DATA_ACTIONS.FETCH_QUERIES_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_QUERIES_SUCCESS; payload: Query[] }
  | { type: typeof DATA_ACTIONS.FETCH_QUERIES_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.EXECUTE_QUERY_REQUEST; payload: string }
  | { type: typeof DATA_ACTIONS.EXECUTE_QUERY_SUCCESS; payload: { queryId: string; results: any[] } }
  | { type: typeof DATA_ACTIONS.EXECUTE_QUERY_FAILURE; payload: { queryId: string; error: string } }
  
  | { type: typeof DATA_ACTIONS.SAVE_QUERY; payload: Query }
  | { type: typeof DATA_ACTIONS.DELETE_QUERY; payload: string }
  
  | { type: typeof DATA_ACTIONS.FETCH_VISUALIZATIONS_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_VISUALIZATIONS_SUCCESS; payload: Visualization[] }
  | { type: typeof DATA_ACTIONS.FETCH_VISUALIZATIONS_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.SAVE_VISUALIZATION; payload: Visualization }
  | { type: typeof DATA_ACTIONS.DELETE_VISUALIZATION; payload: string }
  
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOBS_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOBS_SUCCESS; payload: IngestionJob[] }
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOBS_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOB_DETAILS_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOB_DETAILS_SUCCESS; payload: IngestionJob }
  | { type: typeof DATA_ACTIONS.FETCH_INGESTION_JOB_DETAILS_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.SET_SELECTED_INGESTION_JOB; payload: IngestionJob | null }
  
  | { type: typeof DATA_ACTIONS.START_INGESTION_JOB_REQUEST; payload: string }
  | { type: typeof DATA_ACTIONS.START_INGESTION_JOB_SUCCESS; payload: IngestionJob }
  | { type: typeof DATA_ACTIONS.START_INGESTION_JOB_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.CANCEL_INGESTION_JOB_REQUEST; payload: string }
  | { type: typeof DATA_ACTIONS.CANCEL_INGESTION_JOB_SUCCESS; payload: string }
  | { type: typeof DATA_ACTIONS.CANCEL_INGESTION_JOB_FAILURE; payload: string }
  
  | { type: typeof DATA_ACTIONS.CLEAR_ERROR };

// Initial state
const initialState: DataState = {
  dataSources: [],
  users: [],
  tenants: [],
  datasets: [],
  queries: [],
  visualizations: [],
  ingestionJobs: [],
  selectedDataSource: null,
  selectedDataset: null,
  selectedIngestionJob: null,
  isLoading: false,
  error: null,
  lastUpdated: null,
};

const dataSlice = createSlice({
  name: 'data',
  initialState,
  reducers: {
    // Data sources
    fetchDataSourcesStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchDataSourcesSuccess(state, action: PayloadAction<DataSource[]>) {
      state.dataSources = action.payload;
      state.isLoading = false;
      state.lastUpdated = new Date().toISOString();
    },
    fetchDataSourcesFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    setSelectedDataSource(state, action: PayloadAction<DataSource | null>) {
      state.selectedDataSource = action.payload;
    },
    
    // Users
    fetchUsersStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchUsersSuccess(state, action: PayloadAction<User[]>) {
      state.users = action.payload;
      state.isLoading = false;
      state.lastUpdated = new Date().toISOString();
    },
    fetchUsersFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    
    // Tenants
    fetchTenantsStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchTenantsSuccess(state, action: PayloadAction<Tenant[]>) {
      state.tenants = action.payload;
      state.isLoading = false;
      state.lastUpdated = new Date().toISOString();
    },
    fetchTenantsFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    
    // Generic dataset operations
    fetchDatasetsStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchDatasetsSuccess(state, action: PayloadAction<any[]>) {
      state.datasets = action.payload;
      state.isLoading = false;
      state.lastUpdated = new Date().toISOString();
    },
    fetchDatasetsFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    setSelectedDataset(state, action: PayloadAction<any | null>) {
      state.selectedDataset = action.payload;
    },
    
    // Ingestion jobs
    fetchIngestionJobsStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchIngestionJobsSuccess(state, action: PayloadAction<IngestionJob[]>) {
      state.ingestionJobs = action.payload;
      state.isLoading = false;
      state.lastUpdated = new Date().toISOString();
    },
    fetchIngestionJobsFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    fetchIngestionJobDetailsStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    fetchIngestionJobDetailsSuccess(state, action: PayloadAction<IngestionJob>) {
      state.selectedIngestionJob = action.payload;
      // Also update the job in the jobs array
      const index = state.ingestionJobs.findIndex(job => job.id === action.payload.id);
      if (index !== -1) {
        state.ingestionJobs[index] = action.payload;
      }
      state.isLoading = false;
    },
    fetchIngestionJobDetailsFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    setSelectedIngestionJob(state, action: PayloadAction<IngestionJob | null>) {
      state.selectedIngestionJob = action.payload;
    },
    startIngestionJobStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    startIngestionJobSuccess(state, action: PayloadAction<IngestionJob>) {
      // Add the new job to the jobs array
      state.ingestionJobs.push(action.payload);
      state.isLoading = false;
    },
    startIngestionJobFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    },
    cancelIngestionJobStart(state) {
      state.isLoading = true;
      state.error = null;
    },
    cancelIngestionJobSuccess(state, action: PayloadAction<string>) {
      // Update the job status in the jobs array
      const index = state.ingestionJobs.findIndex(job => job.id === action.payload);
      if (index !== -1) {
        state.ingestionJobs[index].status = 'cancelled';
      }
      // If the selected job is the one being cancelled, update it too
      if (state.selectedIngestionJob && state.selectedIngestionJob.id === action.payload) {
        state.selectedIngestionJob.status = 'cancelled';
      }
      state.isLoading = false;
    },
    cancelIngestionJobFailure(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.error = action.payload;
    }
  },
});

export const dataActions = dataSlice.actions;
export default dataSlice.reducer; 