// Define the data state type
export interface DataState {
  dataSources: DataSource[];
  datasets: Dataset[];
  queries: Query[];
  visualizations: Visualization[];
  selectedDataSource: string | null;
  selectedDataset: string | null;
  isLoading: boolean;
  error: string | null;
  lastUpdated: number | null;
}

// Define data source type
export interface DataSource {
  id: string;
  name: string;
  type: 'postgresql' | 'mysql' | 'mssql' | 'file' | 'api' | 'other';
  description: string;
  connectionStatus: 'connected' | 'disconnected' | 'error';
  lastSync: number | null;
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
  
  CLEAR_ERROR: 'data/clearError',
} as const;

// Define action interfaces
export type DataAction =
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_SUCCESS; payload: DataSource[] }
  | { type: typeof DATA_ACTIONS.FETCH_DATA_SOURCES_FAILURE; payload: string }
  | { type: typeof DATA_ACTIONS.SET_SELECTED_DATA_SOURCE; payload: string | null }
  
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_REQUEST }
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_SUCCESS; payload: Dataset[] }
  | { type: typeof DATA_ACTIONS.FETCH_DATASETS_FAILURE; payload: string }
  | { type: typeof DATA_ACTIONS.SET_SELECTED_DATASET; payload: string | null }
  
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
  
  | { type: typeof DATA_ACTIONS.CLEAR_ERROR };

// Initial state
const initialState: DataState = {
  dataSources: [],
  datasets: [],
  queries: [],
  visualizations: [],
  selectedDataSource: null,
  selectedDataset: null,
  isLoading: false,
  error: null,
  lastUpdated: null,
};

// Create the reducer
export function dataReducer(state: DataState = initialState, action: DataAction): DataState {
  switch (action.type) {
    // Data Sources
    case DATA_ACTIONS.FETCH_DATA_SOURCES_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case DATA_ACTIONS.FETCH_DATA_SOURCES_SUCCESS:
      return {
        ...state,
        dataSources: action.payload,
        isLoading: false,
        error: null,
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.FETCH_DATA_SOURCES_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
    case DATA_ACTIONS.SET_SELECTED_DATA_SOURCE:
      return {
        ...state,
        selectedDataSource: action.payload,
        // Clear selected dataset if changing data source
        selectedDataset: null,
      };

    // Datasets
    case DATA_ACTIONS.FETCH_DATASETS_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case DATA_ACTIONS.FETCH_DATASETS_SUCCESS:
      return {
        ...state,
        datasets: action.payload,
        isLoading: false,
        error: null,
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.FETCH_DATASETS_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
    case DATA_ACTIONS.SET_SELECTED_DATASET:
      return {
        ...state,
        selectedDataset: action.payload,
      };

    // Queries
    case DATA_ACTIONS.FETCH_QUERIES_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case DATA_ACTIONS.FETCH_QUERIES_SUCCESS:
      return {
        ...state,
        queries: action.payload,
        isLoading: false,
        error: null,
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.FETCH_QUERIES_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
    case DATA_ACTIONS.EXECUTE_QUERY_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case DATA_ACTIONS.EXECUTE_QUERY_SUCCESS:
      return {
        ...state,
        queries: state.queries.map(query =>
          query.id === action.payload.queryId
            ? {
                ...query,
                results: action.payload.results,
                lastExecuted: Date.now(),
              }
            : query
        ),
        isLoading: false,
        error: null,
      };
    case DATA_ACTIONS.EXECUTE_QUERY_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload.error,
      };
    case DATA_ACTIONS.SAVE_QUERY:
      const existingQueryIndex = state.queries.findIndex(
        (query) => query.id === action.payload.id
      );
      return {
        ...state,
        queries:
          existingQueryIndex >= 0
            ? [
                ...state.queries.slice(0, existingQueryIndex),
                action.payload,
                ...state.queries.slice(existingQueryIndex + 1),
              ]
            : [...state.queries, action.payload],
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.DELETE_QUERY:
      return {
        ...state,
        queries: state.queries.filter((query) => query.id !== action.payload),
        lastUpdated: Date.now(),
      };

    // Visualizations
    case DATA_ACTIONS.FETCH_VISUALIZATIONS_REQUEST:
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case DATA_ACTIONS.FETCH_VISUALIZATIONS_SUCCESS:
      return {
        ...state,
        visualizations: action.payload,
        isLoading: false,
        error: null,
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.FETCH_VISUALIZATIONS_FAILURE:
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
    case DATA_ACTIONS.SAVE_VISUALIZATION:
      const existingVizIndex = state.visualizations.findIndex(
        (viz) => viz.id === action.payload.id
      );
      return {
        ...state,
        visualizations:
          existingVizIndex >= 0
            ? [
                ...state.visualizations.slice(0, existingVizIndex),
                action.payload,
                ...state.visualizations.slice(existingVizIndex + 1),
              ]
            : [...state.visualizations, action.payload],
        lastUpdated: Date.now(),
      };
    case DATA_ACTIONS.DELETE_VISUALIZATION:
      return {
        ...state,
        visualizations: state.visualizations.filter(
          (viz) => viz.id !== action.payload
        ),
        lastUpdated: Date.now(),
      };

    // Other
    case DATA_ACTIONS.CLEAR_ERROR:
      return {
        ...state,
        error: null,
      };
    default:
      return state;
  }
}

// Action creators
export const dataActions = {
  // Data Sources
  fetchDataSourcesRequest: (): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATA_SOURCES_REQUEST,
  }),
  fetchDataSourcesSuccess: (dataSources: DataSource[]): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATA_SOURCES_SUCCESS,
    payload: dataSources,
  }),
  fetchDataSourcesFailure: (error: string): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATA_SOURCES_FAILURE,
    payload: error,
  }),
  setSelectedDataSource: (dataSourceId: string | null): DataAction => ({
    type: DATA_ACTIONS.SET_SELECTED_DATA_SOURCE,
    payload: dataSourceId,
  }),

  // Datasets
  fetchDatasetsRequest: (): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATASETS_REQUEST,
  }),
  fetchDatasetsSuccess: (datasets: Dataset[]): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATASETS_SUCCESS,
    payload: datasets,
  }),
  fetchDatasetsFailure: (error: string): DataAction => ({
    type: DATA_ACTIONS.FETCH_DATASETS_FAILURE,
    payload: error,
  }),
  setSelectedDataset: (datasetId: string | null): DataAction => ({
    type: DATA_ACTIONS.SET_SELECTED_DATASET,
    payload: datasetId,
  }),

  // Queries
  fetchQueriesRequest: (): DataAction => ({
    type: DATA_ACTIONS.FETCH_QUERIES_REQUEST,
  }),
  fetchQueriesSuccess: (queries: Query[]): DataAction => ({
    type: DATA_ACTIONS.FETCH_QUERIES_SUCCESS,
    payload: queries,
  }),
  fetchQueriesFailure: (error: string): DataAction => ({
    type: DATA_ACTIONS.FETCH_QUERIES_FAILURE,
    payload: error,
  }),
  executeQueryRequest: (queryId: string): DataAction => ({
    type: DATA_ACTIONS.EXECUTE_QUERY_REQUEST,
    payload: queryId,
  }),
  executeQuerySuccess: (queryId: string, results: any[]): DataAction => ({
    type: DATA_ACTIONS.EXECUTE_QUERY_SUCCESS,
    payload: { queryId, results },
  }),
  executeQueryFailure: (queryId: string, error: string): DataAction => ({
    type: DATA_ACTIONS.EXECUTE_QUERY_FAILURE,
    payload: { queryId, error },
  }),
  saveQuery: (query: Query): DataAction => ({
    type: DATA_ACTIONS.SAVE_QUERY,
    payload: query,
  }),
  deleteQuery: (queryId: string): DataAction => ({
    type: DATA_ACTIONS.DELETE_QUERY,
    payload: queryId,
  }),

  // Visualizations
  fetchVisualizationsRequest: (): DataAction => ({
    type: DATA_ACTIONS.FETCH_VISUALIZATIONS_REQUEST,
  }),
  fetchVisualizationsSuccess: (visualizations: Visualization[]): DataAction => ({
    type: DATA_ACTIONS.FETCH_VISUALIZATIONS_SUCCESS,
    payload: visualizations,
  }),
  fetchVisualizationsFailure: (error: string): DataAction => ({
    type: DATA_ACTIONS.FETCH_VISUALIZATIONS_FAILURE,
    payload: error,
  }),
  saveVisualization: (visualization: Visualization): DataAction => ({
    type: DATA_ACTIONS.SAVE_VISUALIZATION,
    payload: visualization,
  }),
  deleteVisualization: (visualizationId: string): DataAction => ({
    type: DATA_ACTIONS.DELETE_VISUALIZATION,
    payload: visualizationId,
  }),

  // Other
  clearError: (): DataAction => ({
    type: DATA_ACTIONS.CLEAR_ERROR,
  }),
}; 