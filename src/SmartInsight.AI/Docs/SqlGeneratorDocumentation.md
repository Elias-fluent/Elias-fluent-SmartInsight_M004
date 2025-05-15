# SmartInsight SQL Generator Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Architecture Overview](#architecture-overview)
3. [Key Components](#key-components)
4. [Usage Guidelines](#usage-guidelines)
5. [Security Best Practices](#security-best-practices)
6. [Examples](#examples)
7. [Troubleshooting](#troubleshooting)
8. [Performance Optimization](#performance-optimization)
9. [API Reference](#api-reference)

## Introduction

The SmartInsight SQL Generator is a secure, efficient system for generating SQL queries from natural language inputs. It provides a robust framework for:

- Converting natural language queries into validated, parameterized SQL
- Ensuring security through multiple validation layers
- Enforcing tenant isolation in a multi-tenant environment
- Optimizing queries for performance
- Comprehensive logging and monitoring

This document serves as a guide for developers using the SQL Generator system, providing details on architecture, usage patterns, security considerations, and best practices.

## Architecture Overview

The SQL Generator system follows a modular architecture with clear separation of concerns:

```
┌─────────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│                     │     │                   │     │                   │
│  Natural Language   │────▶│ Template Selection│────▶│Parameter Extraction│
│  Input              │     │                   │     │                   │
│                     │     └───────────────────┘     └─────────┬─────────┘
└─────────────────────┘                                         │
                                                               │
┌─────────────────────┐     ┌───────────────────┐     ┌─────────▼─────────┐
│                     │     │                   │     │                   │
│  Query Execution    │◀────│ SQL Validation    │◀────│ SQL Generation    │
│                     │     │                   │     │                   │
│                     │     └───────────────────┘     └─────────┬─────────┘
└─────────────────────┘                                         │
        │                                                       │
        │               ┌───────────────────┐                   │
        │               │                   │                   │
        └───────────────▶ Logging Service   │◀──────────────────┘
                        │                   │
                        └───────────────────┘
```

The flow of operations typically follows this sequence:
1. Natural language query is received
2. Template selector identifies the appropriate SQL template
3. Parameter extractor identifies and extracts values from the query
4. SQL Generator combines the template with parameters
5. The generated SQL undergoes validation for security and correctness
6. Tenant scoping is applied to enforce data isolation
7. Query optimization improves performance
8. The SQL is executed and results are returned
9. The entire process is logged for audit and debugging

## Key Components

### Template Repository

The Template Repository stores SQL templates that are used as the basis for generating SQL queries.

**Key Interfaces:**
- `ITemplateRepository`: Manages storage and retrieval of SQL templates
- `ITemplateSelector`: Selects the appropriate template based on intent

**Key Models:**
- `SqlTemplate`: Contains the template text, parameter definitions, and metadata
- `SqlTemplateParameter`: Defines parameters expected by a template

### Parameter Extraction

The Parameter Extraction system identifies and extracts values from natural language inputs.

**Key Interfaces:**
- `IParameterExtractor`: Extracts parameters from natural language
- `IParameterValidator`: Validates extracted parameters

**Key Models:**
- `ExtractedParameter`: Represents a parameter extracted from input
- `ParameterValidationResult`: Results of parameter validation

### SQL Generation

The SQL Generation system combines templates with parameters to create SQL queries.

**Key Interfaces:**
- `ISqlGenerator`: Main interface for generating SQL
- `ISafeSqlGenerator`: Ensures generated SQL is secure

**Key Models:**
- `SqlGenerationResult`: Result of SQL generation process
- `SqlRequest`: Request model for generating SQL

### SQL Validation

The SQL Validation system verifies generated SQL against security and correctness rules.

**Key Interfaces:**
- `ISqlValidator`: Validates SQL for security and correctness
- `ISqlValidationRulesEngine`: Manages validation rules

**Key Models:**
- `SqlValidationResult`: Results of SQL validation
- `SqlValidationIssue`: Represents a specific validation issue
- `SqlValidationRuleDefinition`: Defines a validation rule

### Tenant Scoping

The Tenant Scoping system enforces data isolation in multi-tenant environments.

**Key Interfaces:**
- `ITenantScopingService`: Enforces tenant isolation in SQL

**Key Models:**
- `TenantContext`: Contains tenant information

### Query Optimization

The Query Optimization system improves the performance of generated SQL.

**Key Interfaces:**
- `IQueryOptimizer`: Optimizes SQL queries for performance
- `ISqlQueryOptimizationService`: Provides optimization suggestions

**Key Models:**
- `QueryOptimizationResult`: Results of query optimization
- `QueryComplexityAnalysis`: Analysis of query complexity

### SQL Execution

The SQL Execution system safely executes generated SQL against the database.

**Key Interfaces:**
- `ISqlExecutionService`: Executes SQL and returns results

**Key Models:**
- `SqlExecutionResult`: Results of SQL execution

### Logging

The Logging system records details of the SQL generation and execution process.

**Key Interfaces:**
- `ISqlLoggingService`: Logs SQL generation and execution
- `ISqlLogRetentionService`: Manages log retention

**Key Models:**
- `SqlLogEntry`: Log entry for SQL operations
- `LogRetentionPolicy`: Policy for retaining logs

## Usage Guidelines

### Basic Usage Pattern

The simplest way to use the SQL Generator is through the `ISqlGenerator` interface:

```csharp
// Inject the SqlGenerator
private readonly ISqlGenerator _sqlGenerator;

public MyService(ISqlGenerator sqlGenerator)
{
    _sqlGenerator = sqlGenerator;
}

// Generate SQL from natural language
public async Task<SqlGenerationResult> GenerateFromNaturalLanguage(string query, TenantContext tenantContext)
{
    return await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);
}

// Generate SQL from template with parameters
public async Task<SqlGenerationResult> GenerateFromTemplate(string templateId, Dictionary<string, object> parameters, TenantContext tenantContext)
{
    return await _sqlGenerator.GenerateSqlAsync(templateId, parameters, tenantContext);
}
```

### Template Creation Guidelines

When creating SQL templates:

1. Always use parameter placeholders (`@paramName`) for user inputs
2. Include tenant scoping parameters when appropriate
3. Provide descriptive names and documentation
4. Map templates to intents for natural language matching
5. Define all expected parameters with types and constraints

Example template:

```json
{
  "id": "get_orders_by_date_range",
  "name": "Get Orders By Date Range",
  "description": "Retrieves orders within a specified date range for the current tenant",
  "sqlTemplateText": "SELECT Id, OrderDate, Total, Status FROM Orders WHERE TenantId = @tenantId AND OrderDate BETWEEN @startDate AND @endDate",
  "parameters": [
    {
      "name": "startDate",
      "type": "DateTime",
      "required": true,
      "description": "Start date for order range"
    },
    {
      "name": "endDate",
      "type": "DateTime",
      "required": true,
      "description": "End date for order range"
    },
    {
      "name": "tenantId",
      "type": "Guid",
      "required": true,
      "description": "Tenant ID for isolation",
      "isSystemParameter": true
    }
  ],
  "intentMapping": ["FindOrdersByDate", "GetOrdersInRange", "OrderDateQuery"]
}
```

### Parameter Validation

The system includes robust parameter validation:

1. Type validation ensures parameters match expected types
2. Format validation checks specific formats (email, URL, etc.)
3. Range validation enforces numeric and date constraints
4. Custom validators implement business rules

## Security Best Practices

### SQL Injection Prevention

The system prevents SQL injection through:

1. **Parameterization**: All user inputs are passed as parameters, not interpolated
2. **Input Validation**: Parameters are validated before use
3. **Query Structure Validation**: Generated SQL is validated for structure
4. **Whitelisting**: Only approved templates and operations are allowed

### Never bypass security features:

```csharp
// ❌ NEVER do this
string query = $"SELECT * FROM Users WHERE Username = '{username}'";

// ✅ ALWAYS use parameters
string query = "SELECT * FROM Users WHERE Username = @username";
```

### Multi-Tenant Data Isolation

To ensure proper tenant isolation:

1. Always include TenantContext in requests
2. Never disable tenant scoping for multi-tenant operations
3. Use system parameters for tenant IDs to prevent override
4. Validate tenant permissions for cross-tenant operations

### Validation Rules

The system includes validation rules for:

1. Detecting SQL injection patterns
2. Preventing dangerous operations (DROP, ALTER, etc.)
3. Enforcing tenant isolation
4. Preventing full table scans without filters
5. Detecting sensitive data usage

## Examples

### Basic Query Generation

```csharp
// Natural language query
string query = "find all active users from the marketing department";
var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

// Using template with parameters
var parameters = new Dictionary<string, object>
{
    { "status", "active" },
    { "department", "marketing" }
};
var result = await _sqlGenerator.GenerateSqlAsync("get_users_by_status_and_department", parameters, tenantContext);
```

### Handling Validation Issues

```csharp
var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

if (!result.IsSuccessful)
{
    // Handle error
    logger.LogError("SQL generation failed: {Error}", result.ErrorMessage);
    return BadRequest(result.ErrorMessage);
}

// Check for validation issues
if (result.ValidationResult != null && !result.ValidationResult.IsValid)
{
    // Log validation issues
    foreach (var issue in result.ValidationResult.Issues)
    {
        logger.LogWarning("Validation issue: {Issue} (Severity: {Severity})", 
            issue.Description, issue.Severity);
    }
    
    // For critical issues, prevent execution
    if (result.ValidationResult.HasCriticalIssues())
    {
        return BadRequest("Query contains critical validation issues");
    }
}

// Execute the query
var executionResult = await _sqlExecutionService.ExecuteSqlAsync(result.Sql, result.Parameters);
```

### Query Optimization

```csharp
// Generate SQL
var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

// Optimize the query
var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(result.Sql);

if (optimizationResult.IsOptimized)
{
    // Use the optimized query
    var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
        optimizationResult.OptimizedQuery, result.Parameters);
        
    // Log optimization improvement
    logger.LogInformation("Query optimized with {Percent}% improvement", 
        optimizationResult.EstimatedImprovementPercentage);
}
```

## Troubleshooting

### Common Issues and Solutions

| Issue | Possible Causes | Solutions |
|-------|----------------|-----------|
| Parameter extraction failed | Ambiguous parameters in query | Provide more specific parameter names or values |
| No matching template found | Query intent not recognized | Add template with matching intent or rephrase query |
| SQL validation failed | Security or syntax issues | Check validation errors for specific issues to fix |
| Query optimization failed | Complex query structure | Simplify query or use manual optimization |
| Tenant scoping error | Missing tenant context | Ensure tenant context is provided for all operations |

### Logging and Diagnostics

The SQL Generator system includes comprehensive logging:

1. SQL generation requests and results
2. Parameter extraction details
3. Validation issues
4. Execution performance metrics
5. Security events

Enable detailed logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartInsight.AI.SQL": "Debug"
    }
  }
}
```

## Performance Optimization

### Query Complexity Management

The system includes tools to manage query complexity:

1. `QueryComplexityAnalysis` provides metrics on query complexity
2. `QueryOptimizer` suggests improvements for complex queries
3. Automatic pagination for potentially large result sets

### Optimization Techniques

Common optimization techniques applied by the system:

1. Column selection optimization (avoiding SELECT *)
2. Index utilization improvement
3. Join optimization
4. Filter placement optimization
5. Redundant condition elimination

### Performance Monitoring

Monitor SQL Generator performance using:

1. Execution time metrics in `SqlLogEntry`
2. Complexity scores from `QueryComplexityAnalysis`
3. Optimization improvement percentages

## API Reference

### ISqlGenerator

```csharp
public interface ISqlGenerator
{
    Task<SqlGenerationResult> GenerateSqlFromQueryAsync(string query, TenantContext tenantContext);
    Task<SqlGenerationResult> GenerateSqlAsync(string templateId, Dictionary<string, object> parameters, TenantContext tenantContext);
    Task<SqlGenerationResult> GenerateSqlAsync(SqlTemplate template, Dictionary<string, object> parameters);
    Task<SqlGenerationResult> GenerateParameterizedSqlAsync(SqlTemplate template, Dictionary<string, object> parameters);
}
```

### ISqlValidator

```csharp
public interface ISqlValidator
{
    Task<SqlValidationResult> ValidateSqlAsync(string sql, Dictionary<string, object> parameters = null);
    Task<SqlValidationResult> ValidateSecurityAsync(string sql);
    Task<bool> IsSqlSafeAsync(string sql, Dictionary<string, object> parameters = null);
}
```

### IQueryOptimizer

```csharp
public interface IQueryOptimizer
{
    Task<QueryOptimizationResult> OptimizeQueryAsync(string sql);
    Task<double> GetQueryComplexityAsync(string sql);
    Task<QueryComplexityAnalysis> AnalyzeQueryPerformanceAsync(string sql);
}
```

### ISqlLoggingService

```csharp
public interface ISqlLoggingService
{
    Task LogSqlGenerationAsync(SqlGenerationEvent generationEvent);
    Task LogSqlExecutionAsync(SqlExecutionEvent executionEvent);
    Task LogSqlValidationAsync(SqlValidationEvent validationEvent);
    Task<IReadOnlyList<SqlLogEntry>> GetRecentLogsAsync(int count = 100);
    Task<IReadOnlyList<SqlLogEntry>> GetLogsByQueryTypeAsync(string queryType, DateTime? startTime = null, DateTime? endTime = null);
}
``` 