# SQL Template System Architecture

## Overview

The SQL Template System provides a mechanism for safely generating SQL queries from natural language. It bridges the gap between intent detection/reasoning and actual database access by transforming user intents into parameterized, validated SQL statements. This system ensures security, performance, and multi-tenant isolation while providing a flexible way to map natural language to SQL operations.

## Core Components

### 1. SQL Template Repository

**Purpose:** Store, retrieve, and manage SQL templates

**Components:**
- `ITemplateRepository` - Interface for template storage operations
- `FileSystemTemplateRepository` - Implementation using JSON files on disk 
- `InMemoryTemplateRepository` - Implementation using in-memory dictionary with caching
- `Template` - Model representing a SQL template with placeholders, metadata

**Responsibilities:**
- Template versioning and history
- Template categorization by intent types
- Template validation during registration

### 2. Template Selection Engine

**Purpose:** Match natural language intents to appropriate templates

**Components:**
- `ITemplateSelector` - Interface for template selection strategy
- `IntentBasedTemplateSelector` - Implementation using intent classification
- `CosineSimTemplateSelector` - Implementation using vector embeddings and similarity
- `SelectionResult` - Model representing selected template with confidence score

**Responsibilities:**
- Map user intent to most appropriate template
- Handle ambiguity through confidence scoring
- Support fallback mechanisms for unknown intents

### 3. Parameter Extraction Service

**Purpose:** Extract and validate parameters from natural language for SQL template placeholders

**Components:**
- `IParameterExtractor` - Interface for parameter extraction strategy
- `NLPParameterExtractor` - Implementation using NLP techniques
- `RuleBasedParameterExtractor` - Implementation using regex and rules
- `Parameter` - Model for extracted parameter with type, value, confidence

**Responsibilities:**
- Extract named entities and values from queries
- Convert natural language values to appropriate SQL data types
- Handle parameter validation and sanitization

### 4. SQL Generation Engine

**Purpose:** Generate safe SQL from templates and parameters

**Components:**
- `ISQLGenerator` - Interface for SQL generation
- `SafeSQLGenerator` - Implementation ensuring SQL injection protection
- `ParameterizedSQLGenerator` - Implementation using parameterized queries
- `SQLGenerationResult` - Model for generated SQL with metadata

**Responsibilities:**
- Apply parameters to templates
- Ensure SQL injection prevention
- Generate both parameterized SQL and parameter collections

### 5. Tenant Scoping Service

**Purpose:** Enforce tenant isolation in generated SQL

**Components:**
- `ITenantScopingService` - Interface for tenant scoping operations
- `AutomaticTenantScopingService` - Implementation that adds tenant filters
- `TenantContext` - Model for current tenant context

**Responsibilities:**
- Automatically inject tenant filters into generated SQL
- Prevent cross-tenant data access
- Validate tenant permissions for operations

### 6. SQL Validation Engine

**Purpose:** Validate generated SQL against security and performance rules

**Components:**
- `ISQLValidator` - Interface for SQL validation
- `SecurityValidator` - Implementation for security validation rules
- `PerformanceValidator` - Implementation for performance validation rules
- `ValidationResult` - Model for validation results with issues and severity

**Responsibilities:**
- Check SQL against known injection patterns
- Validate structure and syntax
- Ensure adherence to security best practices
- Check for performance issues

### 7. SQL Execution Service

**Purpose:** Execute validated SQL safely against the database

**Components:**
- `ISQLExecutionService` - Interface for SQL execution
- `DbContextExecutionService` - Implementation using Entity Framework
- `DirectConnectionExecutionService` - Implementation using ADO.NET
- `ExecutionResult` - Model for execution results with metadata

**Responsibilities:**
- Execute SQL with proper parameter handling
- Return structured results
- Handle execution errors

### 8. Query Optimization Service

**Purpose:** Optimize generated SQL for performance

**Components:**
- `IQueryOptimizer` - Interface for query optimization
- `BasicQueryOptimizer` - Implementation with simple optimizations
- `AdvancedQueryOptimizer` - Implementation with advanced analysis
- `OptimizationResult` - Model for optimization results

**Responsibilities:**
- Analyze query plans
- Apply optimization techniques
- Log performance metrics

### 9. Logging Service

**Purpose:** Comprehensive logging of SQL generation and execution

**Components:**
- `ISQLLoggingService` - Interface for SQL-specific logging
- `SQLTelemetryService` - Implementation integrated with telemetry
- `SQLLogEntry` - Model for log entries

**Responsibilities:**
- Log SQL generation requests and results
- Track execution performance
- Record validation issues
- Support audit trail requirements

## Component Interactions and Data Flow

1. User query is processed by `IntentDetector` to determine intent
2. `TemplateSelector` identifies appropriate SQL template based on intent
3. `ParameterExtractor` extracts required parameters from natural language
4. `TenantScopingService` establishes tenant context for isolation
5. `SQLGenerator` applies parameters to template to generate SQL
6. `SQLValidator` validates generated SQL against security and performance rules
7. `QueryOptimizer` applies optimizations to the SQL
8. `SQLExecutionService` executes the query and returns results
9. `SQLLoggingService` logs the entire process with detailed metrics

## Template Storage Structure

Templates will be stored in a structured format that includes:

```json
{
  "templateId": "get_user_by_id",
  "description": "Retrieve user by ID with tenant isolation",
  "intentMapping": ["GetUserDetails", "FindUserById", "UserLookup"],
  "sqlTemplate": "SELECT * FROM Users WHERE Id = @userId AND TenantId = @tenantId",
  "parameters": [
    {
      "name": "userId",
      "type": "Guid",
      "required": true,
      "description": "The unique identifier of the user"
    },
    {
      "name": "tenantId",
      "type": "Guid",
      "required": true,
      "description": "The tenant ID for isolation",
      "isSystemParameter": true
    }
  ],
  "resultType": "User",
  "permissionRequired": "User.Read",
  "version": "1.0",
  "created": "2023-05-15T10:30:00Z",
  "tags": ["user", "core", "identity"]
}
```

## Template Selection Process

1. Extract primary intent and entities from natural language query
2. Look up templates with matching intent mappings
3. For multiple matches, calculate similarity scores between query and templates
4. Select template with highest confidence above threshold
5. If no template exceeds threshold, execute fallback strategy

## Error Handling Strategy

1. **Parameter extraction errors:**
   - If required parameters are missing, request clarification
   - If parameters have ambiguous values, use confidence scoring to determine action

2. **Template selection errors:**
   - If no matching template, log and provide user-friendly error
   - If multiple potential templates with low confidence, request clarification

3. **SQL generation errors:**
   - If template is invalid, log error and notify administrators
   - If generation fails, provide clear error message without exposing internals

4. **Validation errors:**
   - Report security violations and block execution
   - Log performance warnings but allow execution

5. **Execution errors:**
   - Provide sanitized error messages to users
   - Log detailed errors for administrators
   - Support retry mechanisms for transient failures

## Security Considerations

1. **Parameterization:**
   - All user inputs must be parameterized, never interpolated
   - Parameter types are strictly enforced

2. **Tenant Isolation:**
   - Tenant context is always enforced
   - System parameters for tenant scoping cannot be overridden

3. **Validation:**
   - Multiple validation layers (input, SQL structure, execution)
   - Regular security audit of templates

4. **Permissions:**
   - Templates specify required permissions
   - Permission checks before execution

## Performance Considerations

1. **Caching:**
   - Template cache with invalidation strategy
   - Execution plan cache for common queries

2. **Optimization:**
   - Automatic index usage analysis
   - Query complexity assessment
   - Pagination enforcement for large result sets

3. **Metrics:**
   - Execution time tracking
   - Resource usage monitoring
   - Slow query detection and alerting

## Integration with Existing Components

1. **IntentDetector:**
   - Uses intent classification results to select templates
   - Leverages extracted entities for parameter extraction

2. **OllamaClient:**
   - Can be used for advanced parameter extraction and disambiguation
   - Potential use for template generation assistance

3. **Telemetry:**
   - Integrates with SQL logging for comprehensive tracking
   - Helps identify optimization opportunities

4. **Entity Framework:**
   - Option to execute through DbContext for type safety
   - Maps query results to entity types

## Future Extensions

1. **Template Learning:**
   - Analyze successful queries to improve templates
   - Learn from user feedback to refine parameter extraction

2. **Query Builder DSL:**
   - Domain-specific language for complex query building
   - Visual query builder for template creation

3. **Explain Plans:**
   - Generate natural language explanations of SQL operations
   - Help users understand query behavior

4. **Template Versioning:**
   - Support schema migrations with versioned templates
   - Handle backward compatibility

## Implementation Phases

1. **Phase 1: Core Framework**
   - Basic interfaces and models
   - Simple file-based template repository
   - Parameter extraction and SQL generation

2. **Phase 2: Security & Tenant Isolation**
   - Tenant scoping implementation
   - Security validation rules
   - Parameterized query support

3. **Phase 3: Optimization & Execution**
   - Query optimization
   - Execution service
   - Performance metrics

4. **Phase 4: Advanced Features**
   - Template learning
   - Complex parameter extraction
   - Extended validation rules

## Testing Strategy

1. **Unit Tests:**
   - Component-level testing with mocks
   - Test parameter extraction with various inputs
   - Validate security measures

2. **Integration Tests:**
   - End-to-end flow testing
   - Database interaction verification
   - Multi-tenant isolation testing

3. **Security Tests:**
   - Penetration testing with SQL injection attempts
   - Permission boundary verification
   - Data isolation testing

4. **Performance Tests:**
   - Query optimization effectiveness
   - Caching behavior
   - Throughput and latency benchmarks 