# SmartInsight Component Architecture

This document describes the major components of the SmartInsight system and their interactions. The system is designed with a modular architecture that promotes separation of concerns, maintainability, and scalability.

## Component Diagram

```
┌───────────────────────────────────────────────────────────────────────────┐
│                          SmartInsight Platform                            │
│                                                                           │
│  ┌───────────┐      ┌───────────┐      ┌───────────┐      ┌───────────┐  │
│  │           │      │           │      │           │      │           │  │
│  │SmartInsight│      │SmartInsight│      │SmartInsight│      │SmartInsight│  │
│  │    UI     │◄────►│    API    │◄────►│    AI     │◄────►│  Knowledge │  │
│  │           │      │           │      │           │      │           │  │
│  └───────────┘      └─────┬─────┘      └───────────┘      └─────┬─────┘  │
│                          │                                       │        │
│                     ┌────▼────┐                            ┌────▼────┐    │
│                     │         │                            │         │    │
│                     │SmartInsight                          │ Qdrant  │    │
│                     │  Core   │                            │ Vector  │    │
│                     │         │                            │   DB    │    │
│                     └────┬────┘                            └─────────┘    │
│                          │                                                │
│  ┌───────────┐      ┌────▼────┐      ┌───────────┐      ┌───────────┐    │
│  │           │      │         │      │           │      │           │    │
│  │SmartInsight│      │SmartInsight    │SmartInsight│      │SmartInsight│    │
│  │  Admin    │◄────►│  Data    │◄────►│  History  │◄────►│ Telemetry │    │
│  │           │      │         │      │           │      │           │    │
│  └───────────┘      └────┬────┘      └───────────┘      └───────────┘    │
│                          │                                                │
│                     ┌────▼────┐                                           │
│                     │         │                                           │
│                     │PostgreSQL│                                           │
│                     │ Database │                                           │
│                     │         │                                           │
│                     └─────────┘                                           │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

## Component Descriptions

### SmartInsight.UI

**Purpose**: Provides the user-facing web interface for interacting with the system.

**Responsibilities**:
- Render data visualizations and dashboards
- Handle user interactions and input
- Communicate with API for data retrieval and actions
- Implement responsive design for cross-device compatibility
- Manage client-side state and UI rendering

**Dependencies**:
- Depends on SmartInsight.API for all data operations

### SmartInsight.API

**Purpose**: Serves as the primary entry point for client applications, exposing RESTful endpoints.

**Responsibilities**:
- Define API contracts and endpoints
- Handle HTTP requests and responses
- Implement API versioning and documentation
- Manage authentication and authorization
- Route requests to appropriate application services
- Implement rate limiting and request validation

**Dependencies**:
- Depends on SmartInsight.Core for domain models and interfaces
- Depends on SmartInsight.AI for intelligent processing capabilities
- May utilize SmartInsight.Telemetry for logging and monitoring

### SmartInsight.Admin

**Purpose**: Provides administrative functionality for system configuration and management.

**Responsibilities**:
- System configuration management
- User and role management
- Tenant administration
- System health monitoring
- Advanced settings and feature flags

**Dependencies**:
- Depends on SmartInsight.Core for domain models
- Depends on SmartInsight.Data for persistence
- Depends on SmartInsight.API for exposing admin functionality

### SmartInsight.AI

**Purpose**: Contains the AI and machine learning capabilities for data analysis and insight generation.

**Responsibilities**:
- Process and analyze data using AI models
- Generate insights from data patterns
- Implement machine learning algorithms
- Manage model training and deployment
- Handle natural language processing
- Generate vector embeddings for semantic search

**Dependencies**:
- Depends on SmartInsight.Core for domain models
- Depends on SmartInsight.Knowledge for vector database access
- May use SmartInsight.History for historical data analysis

### SmartInsight.Core

**Purpose**: Provides core domain models, interfaces, and shared functionality.

**Responsibilities**:
- Define domain entities and value objects
- Implement domain services and business rules
- Define interfaces for infrastructure services
- Manage validation logic
- Implement cross-cutting domain concerns

**Dependencies**:
- No dependencies on other SmartInsight modules (depends only on standard libraries)

### SmartInsight.Data

**Purpose**: Handles data access and persistence operations.

**Responsibilities**:
- Implement data access patterns (Repository, Unit of Work)
- Manage database context and connections
- Handle database migrations and schema evolution
- Implement data caching strategies
- Provide data mapping between domain and persistence models

**Dependencies**:
- Depends on SmartInsight.Core for domain models and interfaces
- External dependency on PostgreSQL database

### SmartInsight.History

**Purpose**: Manages historical data and tracks user interactions.

**Responsibilities**:
- Store and retrieve historical data
- Track user interactions and queries
- Provide data for trend analysis
- Implement data archiving and retention policies
- Support time-series analysis

**Dependencies**:
- Depends on SmartInsight.Core for domain models
- Depends on SmartInsight.Data for persistence

### SmartInsight.Knowledge

**Purpose**: Manages the knowledge base and vector database operations.

**Responsibilities**:
- Store and retrieve vector embeddings
- Manage semantic search functionality
- Organize and structure knowledge information
- Handle similarity searches
- Store and retrieve reference data

**Dependencies**:
- Depends on SmartInsight.Core for domain models
- External dependency on Qdrant vector database

### SmartInsight.Telemetry

**Purpose**: Provides logging, monitoring, and telemetry functionality.

**Responsibilities**:
- Implement structured logging
- Monitor system performance and health
- Collect usage metrics and statistics
- Track errors and exceptions
- Support observability and diagnostics

**Dependencies**:
- Minimal dependencies, typically only on SmartInsight.Core for basic models

## Key Interfaces

The following interfaces represent the primary contracts between components:

### IDataRepository

Interface for data access operations, implemented by SmartInsight.Data:

```csharp
public interface IDataRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

### IVectorStore

Interface for vector database operations, implemented by SmartInsight.Knowledge:

```csharp
public interface IVectorStore<T> where T : class
{
    Task<Guid> StoreVectorAsync(T item, float[] vector, Dictionary<string, object> metadata = null);
    Task<IEnumerable<(T item, float score)>> SearchAsync(float[] queryVector, int limit = 10);
    Task DeleteAsync(Guid id);
}
```

### IAnalyticsEngine

Interface for AI analytics operations, implemented by SmartInsight.AI:

```csharp
public interface IAnalyticsEngine
{
    Task<AnalysisResult> AnalyzeDataAsync(AnalysisRequest request);
    Task<InsightCollection> GenerateInsightsAsync(DataSet dataSet);
    Task<float[]> GenerateEmbeddingAsync(string text);
}
```

### ITelemetryService

Interface for telemetry operations, implemented by SmartInsight.Telemetry:

```csharp
public interface ITelemetryService
{
    void TrackEvent(string eventName, Dictionary<string, string> properties = null);
    void TrackDependency(string dependencyType, string target, string name, DateTimeOffset startTime, TimeSpan duration, bool success);
    void TrackException(Exception exception, Dictionary<string, string> properties = null);
    void TrackTrace(string message, SeverityLevel severityLevel, Dictionary<string, string> properties = null);
}
```

## Component Interaction Examples

### 1. User Query Processing

1. User submits a natural language query via the UI
2. UI sends the query to the API
3. API forwards the query to AI component
4. AI component:
   - Generates vector embedding for the query
   - Uses Knowledge component to find similar vectors
   - Processes the results
   - Returns structured insights
5. API returns the response to UI
6. UI renders the results to the user

### 2. Data Analysis Workflow

1. System receives new data from an external source
2. API receives the data and forwards to appropriate services
3. Data component stores the raw data in PostgreSQL
4. AI component analyzes the data for insights
5. Knowledge component stores vectorized data in Qdrant
6. History component records the data processing event
7. Telemetry component logs the entire operation
8. The insights become available for querying via the API

## Component Dependencies Matrix

The following matrix shows the dependencies between components:

| Component          | Dependencies                                           |
|--------------------|--------------------------------------------------------|
| SmartInsight.UI    | SmartInsight.API                                       |
| SmartInsight.API   | SmartInsight.Core, SmartInsight.AI, SmartInsight.Telemetry |
| SmartInsight.Admin | SmartInsight.Core, SmartInsight.Data, SmartInsight.API |
| SmartInsight.AI    | SmartInsight.Core, SmartInsight.Knowledge, SmartInsight.History |
| SmartInsight.Core  | (none)                                                 |
| SmartInsight.Data  | SmartInsight.Core, PostgreSQL                          |
| SmartInsight.History | SmartInsight.Core, SmartInsight.Data                 |
| SmartInsight.Knowledge | SmartInsight.Core, Qdrant                          |
| SmartInsight.Telemetry | SmartInsight.Core                                  |
</rewritten_file> 