// Define RootState type for the store
export interface RootState {
  // Add actual state slices as needed
  dataSources: {
    items: any[];
    loading: boolean;
    error: string | null;
  };
  // Add other state types here
} 