# SQL Generator Quick Reference

## Key Interfaces

| Interface | Purpose | Primary Methods |
|-----------|---------|----------------|
| `ISqlGenerator` | Main entry point for SQL generation | `GenerateSqlFromQueryAsync()`, `GenerateSqlAsync()` |
| `ISqlValidator` | Validates SQL for security and correctness | `ValidateSqlAsync()`, `IsSqlSafeAsync()` |
| `IQueryOptimizer` | Optimizes SQL for performance | `OptimizeQueryAsync()`, `GetQueryComplexityAsync()` |
| `ISqlLoggingService` | Logs SQL operations | `LogSqlGenerationAsync()`, `GetRecentLogsAsync()` |
| `ITenantScopingService` | Enforces tenant isolation | `ApplyTenantScopingAsync()` |

## Common Usage Patterns

### Generate SQL from Natural Language

```csharp
// Basic usage
var result = await _sqlGenerator.GenerateSqlFromQueryAsync(
    "find all active users in marketing department", 
    tenantContext);

// Check success
if (result.IsSuccessful) {
    // Use result.Sql and result.Parameters
}
```

### Generate SQL from Template

```csharp
// Using template ID with parameters
var parameters = new Dictionary<string, object> {
    { "status", "active" },
    { "department", "marketing" }
};

var result = await _sqlGenerator.GenerateSqlAsync(
    "get_users_by_status_and_department", 
    parameters, 
    tenantContext);
```

### Validate and Execute SQL

```csharp
// Generate SQL
var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

// Validate SQL
if (result.ValidationResult != null && !result.ValidationResult.IsValid) {
    // Handle validation issues
    var hasCritical = result.ValidationResult.Issues.Any(i => 
        i.Severity == ValidationSeverity.Critical);
    
    if (hasCritical) {
        return BadRequest("Security validation failed");
    }
}

// Execute SQL
var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
    result.Sql, 
    result.Parameters);
```

### Optimize SQL Query

```csharp
// Optimize query
var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(result.Sql);

if (optimizationResult.IsOptimized) {
    // Use optimized query
    result.Sql = optimizationResult.OptimizedQuery;
}
```

## Security Guidelines

1. **Always use parameters** - Never concatenate values
2. **Include tenant context** - Required for multi-tenant isolation
3. **Validate all SQL** - Check `IsSqlSafeAsync()` before execution
4. **Log security events** - Use `LogSqlValidationAsync()` for security issues
5. **Handle critical issues** - Never execute SQL with critical validation issues

## Template Checklist

When creating SQL templates:

- [ ] Use parameter placeholders for all variables
- [ ] Include tenant isolation (WHERE TenantId = @tenantId)
- [ ] Map to relevant intents for NL matching
- [ ] Define all parameters with types and constraints
- [ ] Add meaningful description and documentation
- [ ] Tag with appropriate categories
- [ ] Test with validation to ensure security

## Common Model Properties

**SqlGenerationResult**:
- `IsSuccessful` - Whether generation succeeded
- `Sql` - The generated SQL text
- `Parameters` - Dictionary of parameter values
- `ValidationResult` - Results of validation
- `ErrorMessage` - Error message if generation failed

**SqlValidationResult**:
- `IsValid` - Whether validation passed
- `Issues` - List of validation issues
- `HasCriticalIssues()` - Whether critical issues exist

**QueryOptimizationResult**:
- `IsOptimized` - Whether optimization succeeded
- `OptimizedQuery` - The optimized SQL
- `EstimatedImprovementPercentage` - Estimated performance improvement
- `Explanation` - Explanation of optimizations

## Common Error Codes

| Code | Description | Solution |
|------|-------------|----------|
| `SG-001` | No matching template | Add template or use direct ID |
| `SG-002` | Missing required parameter | Provide all required parameters |
| `SG-003` | Parameter type mismatch | Ensure correct parameter types |
| `SG-004` | SQL injection detected | Validate user input |
| `SG-005` | Missing tenant context | Always provide tenant context |
| `SG-006` | Tenant isolation violation | Ensure proper tenant filters |
| `SG-007` | Unsupported SQL operation | Use only supported operations |
| `SG-008` | Query optimization failed | Simplify query or use manual tuning |

## Dependency Injection Setup

```csharp
// In Startup.ConfigureServices
services.AddSqlGeneratorServices(Configuration);

// OR with custom options
services.AddSqlGeneratorServices(options => {
    options.EnableLogging = true;
    options.EnableQueryOptimization = true;
    options.DefaultPageSize = 100;
    options.MaxComplexityAllowed = 8;
    options.StrictValidation = true;
});
```

## Resources

- [Full Documentation](./SqlGeneratorDocumentation.md)
- [Troubleshooting Guide](./SqlGeneratorTroubleshooting.md)
- [SQL Template Architecture](./SQLTemplateSystemArchitecture.md) 