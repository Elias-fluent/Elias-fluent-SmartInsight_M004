# SQL Generator Troubleshooting Guide

This document provides developer-focused troubleshooting steps for common issues encountered when using the SmartInsight SQL Generator system.

## Common Errors and Solutions

### Template Selection Failures

**Issue**: No template found or low confidence match.

**Possible Causes**:
- Natural language query doesn't match any intent patterns
- Ambiguous query mapping to multiple templates
- Missing templates for the specific use case

**Solutions**:
1. Check available templates with `_templateRepository.GetAllTemplatesAsync()`
2. Add new template with appropriate intent mappings
3. Log the failing query and analyze patterns
4. Use template ID directly instead of natural language if needed

```csharp
// Fallback to direct template usage
if (nlResult == null || !nlResult.IsSuccessful)
{
    return await _sqlGenerator.GenerateSqlAsync(
        "fallback_template_id", parameters, tenantContext);
}
```

### Parameter Extraction Issues

**Issue**: Missing or invalid parameters extracted from natural language.

**Possible Causes**:
- Parameter names not recognized in input
- Value type mismatch (e.g., text provided for date)
- Required parameters missing

**Solutions**:
1. Check extracted parameters with debug logging
2. Provide default values for optional parameters
3. Add parameter aliases to improve extraction
4. Use explicit parameter dictionary instead of extraction

```csharp
// Enable detailed parameter extraction logging
services.Configure<SqlGeneratorOptions>(options => 
{
    options.LogLevel = LogLevel.Debug;
    options.LogParameterExtraction = true;
});

// Provide explicit parameters instead of relying on extraction
var parameters = new Dictionary<string, object>
{
    { "status", "active" },
    { "department", "marketing" },
    { "startDate", DateTime.Today.AddDays(-30) }
};
```

### Validation Failures

**Issue**: SQL validation fails with security or structural errors.

**Possible Causes**:
- SQL injection attempt detected
- Template contains unsafe patterns
- User input contains malicious patterns
- Generated SQL violates tenant isolation

**Solutions**:
1. Inspect validation issues using `ValidationResult.Issues`
2. Update template to use parameterization properly
3. Sanitize input before processing
4. Check if tenant context is properly provided

```csharp
// Check validation issues and their severity
if (!result.ValidationResult.IsValid)
{
    foreach (var issue in result.ValidationResult.Issues)
    {
        logger.LogWarning("SQL validation issue: {Description} ({Category}, {Severity})",
            issue.Description, issue.Category, issue.Severity);
            
        // You may choose to continue for non-critical issues
        if (issue.Severity != ValidationSeverity.Critical)
        {
            continue;
        }
        
        return BadRequest($"Security issue: {issue.Description}");
    }
}
```

### Tenant Isolation Problems

**Issue**: Cross-tenant data access or missing tenant context.

**Possible Causes**:
- TenantContext not provided or null
- Template missing tenant scoping
- Custom SQL bypassing tenant filters

**Solutions**:
1. Always provide valid TenantContext
2. Ensure templates have tenant filters
3. Verify tenant scoping service is registered
4. Check logs for tenant context issues

```csharp
// Ensure tenant context is always provided
if (tenantContext == null || tenantContext.TenantId == Guid.Empty)
{
    throw new ArgumentException("Valid tenant context is required", nameof(tenantContext));
}

// For admin operations with cross-tenant capability
if (requiredPermission == "Admin.CrossTenant")
{
    using var scope = logger.BeginScope("Cross-tenant operation requested");
    logger.LogWarning("User {UserId} performing cross-tenant operation", userId);
    
    // Additional authorization checks should be performed
    var isAuthorized = await _authService.HasPermissionAsync(userId, requiredPermission);
    if (!isAuthorized)
    {
        throw new UnauthorizedAccessException("Cross-tenant operations require admin permission");
    }
}
```

### Performance Issues

**Issue**: Generated SQL queries have poor performance.

**Possible Causes**:
- Missing indexes on filtered columns
- Full table scans in queries
- Inefficient joins or subqueries
- Large result sets without pagination

**Solutions**:
1. Use Query Optimizer to analyze and improve queries
2. Add missing indexes to database
3. Enforce pagination for large result sets
4. Update templates with more efficient SQL patterns

```csharp
// Analyze query complexity and optimize if needed
var complexity = await _queryOptimizer.GetQueryComplexityAsync(result.Sql);
if (complexity > 7) // High complexity threshold
{
    logger.LogWarning("High complexity query detected: {Complexity}", complexity);
    
    // Optimize the query
    var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(result.Sql);
    if (optimizationResult.IsOptimized)
    {
        // Use the optimized query instead
        result.Sql = optimizationResult.OptimizedQuery;
        logger.LogInformation("Query optimized, improvement: {Percent}%", 
            optimizationResult.EstimatedImprovementPercentage);
    }
}

// Enforce pagination
if (!result.Sql.Contains("TOP ", StringComparison.OrdinalIgnoreCase) && 
    !result.Sql.Contains("LIMIT ", StringComparison.OrdinalIgnoreCase))
{
    // Add pagination if not present
    result.Sql = _sqlSanitizer.AddPagination(result.Sql, 100); // Default page size
    logger.LogInformation("Pagination added to query");
}
```

## Diagnostic Logging

Enable comprehensive diagnostic logging with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartInsight.AI.SQL": "Debug",
      "SmartInsight.AI.SQL.ParameterExtractor": "Trace",
      "SmartInsight.AI.SQL.Validators": "Debug"
    }
  },
  "SqlGeneratorOptions": {
    "LogParameterExtraction": true,
    "LogTemplateSelection": true,
    "LogValidationDetails": true,
    "LogGeneratedSql": true
  }
}
```

### Useful Diagnostic Queries

Retrieve recent logs for SQL generation:

```csharp
// Get recent SQL generation logs to diagnose issues
var logs = await _sqlLoggingService.GetRecentLogsAsync(30);

// Filter logs by error status
var errorLogs = logs.Where(l => l.Status == LogStatus.Error || l.Status == LogStatus.Warning);

// Get logs for a specific query type
var userQueryLogs = await _sqlLoggingService.GetLogsByQueryTypeAsync(
    "UserQuery", 
    DateTime.UtcNow.AddDays(-1),
    DateTime.UtcNow);
```

## Debug Tracing

For detailed troubleshooting, enable debug trace:

```csharp
// Include one-time debug tracing for a specific query
var builder = new SqlRequestBuilder()
    .WithTemplateName("get_users_by_department")
    .WithParameter("department", department)
    .WithTenantContext(tenantContext)
    .WithDebugTrace(true); // Enable detailed tracing
    
var request = builder.Build();
var result = await _sqlGenerator.GenerateSqlAsync(request);

// Debug trace will be included in result
if (result.DebugTrace != null)
{
    logger.LogDebug("Template selection trace: {Trace}", result.DebugTrace.TemplateSelectionTrace);
    logger.LogDebug("Parameter extraction trace: {Trace}", result.DebugTrace.ParameterExtractionTrace);
    logger.LogDebug("Validation trace: {Trace}", result.DebugTrace.ValidationTrace);
}
```

## Security Recommendations

When troubleshooting SQL Generator issues:

1. **NEVER** disable validation to "fix" issues - address the root cause
2. **NEVER** use string interpolation to insert values into SQL
3. **NEVER** modify generated SQL directly without re-validating
4. **NEVER** expose validation errors directly to end users (log them and show sanitized messages)
5. **ALWAYS** maintain tenant isolation
6. **ALWAYS** log security-related issues

## Performance Tuning Checklist

If experiencing performance issues:

1. ✅ Check query complexity scores (target < 5)
2. ✅ Verify indexes exist for filtered columns
3. ✅ Ensure pagination is applied for large tables
4. ✅ Look for missing JOIN conditions
5. ✅ Verify parameters are properly typed (e.g., GUIDs not stored as strings)
6. ✅ Check for excessive data returned (SELECT * vs. specific columns)
7. ✅ Monitor SQL execution time metrics in logs

## Contact and Support

For internal support with the SQL Generator system:

- File GitHub issues: `https://github.com/smartinsight/sqlgenerator/issues`
- Contact the data platform team: `data-platform@smartinsight.com`
- API documentation: `https://docs.internal.smartinsight.com/sql-generator-api` 