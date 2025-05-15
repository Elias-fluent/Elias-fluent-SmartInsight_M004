# SQL Generator Code Examples

This document provides practical code examples for common SQL Generator usage scenarios.

## Basic Usage Examples

### 1. Generate SQL from Natural Language Query

```csharp
public class UserQueryService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ISqlExecutionService _sqlExecutionService;
    private readonly ILogger<UserQueryService> _logger;

    public UserQueryService(
        ISqlGenerator sqlGenerator,
        ISqlExecutionService sqlExecutionService,
        ILogger<UserQueryService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _sqlExecutionService = sqlExecutionService;
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> ExecuteNaturalLanguageQueryAsync(
        string query, 
        TenantContext tenantContext)
    {
        // Generate SQL from natural language query
        var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Failed to generate SQL: {ErrorMessage}", result.ErrorMessage);
            throw new ApplicationException($"Failed to generate SQL: {result.ErrorMessage}");
        }

        _logger.LogInformation("Generated SQL: {Sql}", result.Sql);

        // Execute the generated SQL
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
            result.Sql, 
            result.Parameters);

        return executionResult.Results;
    }
}
```

### 2. Generate SQL from Template

```csharp
public class OrderService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ISqlExecutionService _sqlExecutionService;

    public OrderService(
        ISqlGenerator sqlGenerator,
        ISqlExecutionService sqlExecutionService)
    {
        _sqlGenerator = sqlGenerator;
        _sqlExecutionService = sqlExecutionService;
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        TenantContext tenantContext)
    {
        // Create parameters dictionary
        var parameters = new Dictionary<string, object>
        {
            { "startDate", startDate },
            { "endDate", endDate }
        };

        // Generate SQL using template
        var result = await _sqlGenerator.GenerateSqlAsync(
            "get_orders_by_date_range", 
            parameters, 
            tenantContext);

        if (!result.IsSuccessful)
        {
            throw new ApplicationException($"Failed to generate SQL: {result.ErrorMessage}");
        }

        // Execute the SQL and map to Order objects
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync<Order>(
            result.Sql, 
            result.Parameters);

        return executionResult.Results;
    }
}
```

### 3. Using SQL Generation with Validation

```csharp
public class ProductQueryService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ISqlValidator _sqlValidator;
    private readonly ISqlExecutionService _sqlExecutionService;
    private readonly ILogger<ProductQueryService> _logger;

    public ProductQueryService(
        ISqlGenerator sqlGenerator,
        ISqlValidator sqlValidator,
        ISqlExecutionService sqlExecutionService,
        ILogger<ProductQueryService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _sqlValidator = sqlValidator;
        _sqlExecutionService = sqlExecutionService;
        _logger = logger;
    }

    public async Task<IActionResult> GetProductsAsync(
        string query, 
        TenantContext tenantContext)
    {
        // Generate SQL from the query
        var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

        if (!result.IsSuccessful)
        {
            return new BadRequestObjectResult($"Failed to generate SQL: {result.ErrorMessage}");
        }

        // Validate generated SQL
        var validationResult = await _sqlValidator.ValidateSqlAsync(result.Sql, result.Parameters);

        if (!validationResult.IsValid)
        {
            // Log all validation issues
            foreach (var issue in validationResult.Issues)
            {
                _logger.LogWarning("Validation issue: {Description} ({Category}, {Severity})",
                    issue.Description, issue.Category, issue.Severity);
            }

            // If there are critical issues, return error
            if (validationResult.Issues.Any(i => i.Severity == ValidationSeverity.Critical))
            {
                return new BadRequestObjectResult("The query contains security issues and cannot be executed");
            }
        }

        // Execute the validated SQL
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
            result.Sql, 
            result.Parameters);

        return new OkObjectResult(executionResult.Results);
    }
}
```

## Advanced Examples

### 4. Using Query Optimization

```csharp
public class ReportService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IQueryOptimizer _queryOptimizer;
    private readonly ISqlExecutionService _sqlExecutionService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ISqlGenerator sqlGenerator,
        IQueryOptimizer queryOptimizer,
        ISqlExecutionService sqlExecutionService,
        ILogger<ReportService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _queryOptimizer = queryOptimizer;
        _sqlExecutionService = sqlExecutionService;
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> GenerateReportAsync(
        string reportType, 
        Dictionary<string, object> parameters, 
        TenantContext tenantContext)
    {
        // Generate SQL for the report
        var result = await _sqlGenerator.GenerateSqlAsync(
            $"report_{reportType}", 
            parameters, 
            tenantContext);

        if (!result.IsSuccessful)
        {
            throw new ApplicationException($"Failed to generate report SQL: {result.ErrorMessage}");
        }

        // For reports, analyze query complexity
        var complexity = await _queryOptimizer.GetQueryComplexityAsync(result.Sql);
        _logger.LogInformation("Report query complexity: {Complexity}", complexity);

        // For complex queries, apply optimization
        if (complexity > 5) // Threshold for optimization
        {
            _logger.LogInformation("Query complexity above threshold, optimizing");
            
            var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(result.Sql);
            
            if (optimizationResult.IsOptimized)
            {
                _logger.LogInformation(
                    "Query optimized with {Percent}% estimated improvement",
                    optimizationResult.EstimatedImprovementPercentage);
                    
                result.Sql = optimizationResult.OptimizedQuery;
            }
            else
            {
                _logger.LogInformation("No optimization available: {Reason}", 
                    optimizationResult.Explanation);
            }
        }

        // Execute the optimized SQL
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
            result.Sql, 
            result.Parameters);

        return executionResult.Results;
    }
}
```

### 5. Handling Tenant Isolation

```csharp
public class DataAdminService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ITenantScopingService _tenantScopingService;
    private readonly ISqlExecutionService _sqlExecutionService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<DataAdminService> _logger;

    public DataAdminService(
        ISqlGenerator sqlGenerator,
        ITenantScopingService tenantScopingService,
        ISqlExecutionService sqlExecutionService,
        IAuthorizationService authorizationService,
        ILogger<DataAdminService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _tenantScopingService = tenantScopingService;
        _sqlExecutionService = sqlExecutionService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<IActionResult> ExecuteAdminQueryAsync(
        string query, 
        TenantContext tenantContext,
        bool allowCrossTenant,
        string userId)
    {
        // Generate SQL
        var result = await _sqlGenerator.GenerateSqlFromQueryAsync(query, tenantContext);

        if (!result.IsSuccessful)
        {
            return new BadRequestObjectResult($"Failed to generate SQL: {result.ErrorMessage}");
        }

        // For cross-tenant operations, special handling required
        if (allowCrossTenant)
        {
            // First check if user is authorized for cross-tenant operations
            bool isAuthorized = await _authorizationService.IsAuthorizedAsync(
                userId, 
                "Admin.CrossTenant");

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "Unauthorized cross-tenant operation attempted by user {UserId}", 
                    userId);
                    
                return new ForbidResult("User not authorized for cross-tenant operations");
            }

            _logger.LogWarning(
                "Cross-tenant operation authorized for user {UserId}", 
                userId);

            // Remove tenant scoping for authorized admin operations
            var unscopedSql = await _tenantScopingService.RemoveTenantScopingAsync(
                result.Sql, 
                result.Parameters);
                
            result.Sql = unscopedSql;
        }
        else
        {
            // Ensure tenant scoping is properly applied
            var scopingResult = await _tenantScopingService.EnsureTenantScopingAsync(
                result.Sql, 
                result.Parameters, 
                tenantContext);
                
            if (!scopingResult.IsScopingValid)
            {
                _logger.LogError("Tenant scoping validation failed: {Reason}", 
                    scopingResult.ErrorMessage);
                    
                return new BadRequestObjectResult(
                    "The query cannot be executed because it would violate tenant isolation");
            }
        }

        // Execute the SQL with appropriate scoping
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
            result.Sql, 
            result.Parameters);

        return new OkObjectResult(executionResult.Results);
    }
}
```

### 6. Using the SQL Logging Service

```csharp
public class DatabaseAuditService
{
    private readonly ISqlLoggingService _sqlLoggingService;
    private readonly ILogger<DatabaseAuditService> _logger;

    public DatabaseAuditService(
        ISqlLoggingService sqlLoggingService,
        ILogger<DatabaseAuditService> logger)
    {
        _sqlLoggingService = sqlLoggingService;
        _logger = logger;
    }

    public async Task<AuditReport> GenerateAuditReportAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        // Get all SQL logs for the period
        var logs = await _sqlLoggingService.GetLogsBetweenDatesAsync(
            startDate, 
            endDate);

        _logger.LogInformation(
            "Retrieved {Count} SQL logs for audit between {StartDate} and {EndDate}",
            logs.Count,
            startDate,
            endDate);

        // Get security-related logs
        var securityLogs = logs
            .Where(l => l.Category == LogCategory.Security)
            .ToList();

        // Get logs with critical severity
        var criticalLogs = logs
            .Where(l => l.Severity == LogSeverity.Critical)
            .ToList();

        // Get logs by operation type
        var selectLogs = logs
            .Where(l => l.OperationType == SqlOperationType.Select)
            .ToList();
            
        var updateLogs = logs
            .Where(l => l.OperationType == SqlOperationType.Update)
            .ToList();
            
        var deleteLogs = logs
            .Where(l => l.OperationType == SqlOperationType.Delete)
            .ToList();

        // Get failed operations
        var failedLogs = logs
            .Where(l => !l.IsSuccessful)
            .ToList();

        // Aggregate performance metrics
        var avgExecutionTime = logs
            .Where(l => l.ExecutionTime.HasValue)
            .Average(l => l.ExecutionTime.Value);

        // Generate report
        return new AuditReport
        {
            TotalOperations = logs.Count,
            SecurityIncidents = securityLogs.Count,
            CriticalIncidents = criticalLogs.Count,
            SelectOperations = selectLogs.Count,
            UpdateOperations = updateLogs.Count,
            DeleteOperations = deleteLogs.Count,
            FailedOperations = failedLogs.Count,
            AverageExecutionTimeMs = avgExecutionTime,
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
```

### 7. Using Template Repository

```csharp
public class TemplateManagementService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly ISqlValidator _sqlValidator;
    private readonly ILogger<TemplateManagementService> _logger;

    public TemplateManagementService(
        ITemplateRepository templateRepository,
        ISqlValidator sqlValidator,
        ILogger<TemplateManagementService> logger)
    {
        _templateRepository = templateRepository;
        _sqlValidator = sqlValidator;
        _logger = logger;
    }

    public async Task<IActionResult> AddTemplateAsync(SqlTemplate template)
    {
        // Validate template SQL for security
        var validationResult = await _sqlValidator.ValidateSqlAsync(template.SqlTemplateText);

        if (!validationResult.IsValid)
        {
            var criticalIssues = validationResult.Issues
                .Where(i => i.Severity == ValidationSeverity.Critical)
                .ToList();

            if (criticalIssues.Any())
            {
                _logger.LogError(
                    "Template {TemplateName} has critical security issues and cannot be added",
                    template.Name);
                    
                return new BadRequestObjectResult(
                    $"Template contains critical security issues: {string.Join(", ", criticalIssues.Select(i => i.Description))}");
            }

            // Log non-critical issues
            foreach (var issue in validationResult.Issues)
            {
                _logger.LogWarning(
                    "Template {TemplateName} has validation issue: {Issue} ({Severity})",
                    template.Name,
                    issue.Description,
                    issue.Severity);
            }
        }

        // Validate parameter definitions
        if (template.Parameters == null || !template.Parameters.Any())
        {
            _logger.LogWarning(
                "Template {TemplateName} has no parameters defined",
                template.Name);
                
            return new BadRequestObjectResult("Template must define at least one parameter");
        }

        // Ensure template has proper tenant scoping
        if (!TemplateHasTenantScoping(template))
        {
            _logger.LogWarning(
                "Template {TemplateName} does not have tenant scoping",
                template.Name);
                
            return new BadRequestObjectResult(
                "Template must include tenant scoping (TenantId parameter and filter)");
        }

        // Add the template to the repository
        await _templateRepository.AddTemplateAsync(template);
        
        _logger.LogInformation(
            "Template {TemplateId} '{TemplateName}' added successfully",
            template.Id,
            template.Name);
            
        return new OkObjectResult(template);
    }

    private bool TemplateHasTenantScoping(SqlTemplate template)
    {
        // Check for tenant parameter definition
        bool hasTenantParameter = template.Parameters
            .Any(p => p.Name.Equals("tenantId", StringComparison.OrdinalIgnoreCase));

        // Check for tenant filter in SQL template text
        bool hasTenantFilter = template.SqlTemplateText
            .Contains("TenantId = @tenantId", StringComparison.OrdinalIgnoreCase) || 
            template.SqlTemplateText
            .Contains("TenantId=@tenantId", StringComparison.OrdinalIgnoreCase);

        return hasTenantParameter && hasTenantFilter;
    }
}
```

## Utility Examples

### 8. Custom Parameter Handling

```csharp
public class CustomParameterService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterExtractor _parameterExtractor;
    private readonly ISqlExecutionService _sqlExecutionService;

    public CustomParameterService(
        ISqlGenerator sqlGenerator,
        IParameterExtractor parameterExtractor,
        ISqlExecutionService sqlExecutionService)
    {
        _sqlGenerator = sqlGenerator;
        _parameterExtractor = parameterExtractor;
        _sqlExecutionService = sqlExecutionService;
    }

    public async Task<IEnumerable<dynamic>> ExecuteWithCustomParametersAsync(
        string query, 
        TenantContext tenantContext)
    {
        // Extract parameters from the natural language query
        var extractionResult = await _parameterExtractor.ExtractParametersAsync(query);

        if (!extractionResult.IsSuccessful)
        {
            throw new ApplicationException(
                $"Failed to extract parameters: {extractionResult.ErrorMessage}");
        }

        // Custom parameter handling - apply business logic
        var parameters = new Dictionary<string, object>();

        foreach (var param in extractionResult.Parameters)
        {
            // Apply custom transformations or business rules
            if (param.Name.Equals("date", StringComparison.OrdinalIgnoreCase) && 
                param.Value is string dateStr)
            {
                // Convert relative dates to absolute dates
                if (dateStr.Equals("today", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(param.Name, DateTime.Today);
                }
                else if (dateStr.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(param.Name, DateTime.Today.AddDays(-1));
                }
                else if (dateStr.Equals("last week", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add("startDate", DateTime.Today.AddDays(-7));
                    parameters.Add("endDate", DateTime.Today);
                }
                else
                {
                    // Try to parse as regular date
                    if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        parameters.Add(param.Name, parsedDate);
                    }
                }
            }
            else if (param.Name.Equals("status", StringComparison.OrdinalIgnoreCase) && 
                     param.Value is string statusStr)
            {
                // Map status synonyms
                if (statusStr.Equals("done", StringComparison.OrdinalIgnoreCase) ||
                    statusStr.Equals("completed", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(param.Name, "Completed");
                }
                else if (statusStr.Equals("in progress", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(param.Name, "InProgress");
                }
                else if (statusStr.Equals("pending", StringComparison.OrdinalIgnoreCase) ||
                         statusStr.Equals("waiting", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(param.Name, "Pending");
                }
                else
                {
                    parameters.Add(param.Name, statusStr);
                }
            }
            else
            {
                // Use parameter as-is
                parameters.Add(param.Name, param.Value);
            }
        }

        // Generate SQL with custom parameters
        var result = await _sqlGenerator.GenerateSqlAsync(
            extractionResult.TemplateId, 
            parameters, 
            tenantContext);

        if (!result.IsSuccessful)
        {
            throw new ApplicationException($"Failed to generate SQL: {result.ErrorMessage}");
        }

        // Execute SQL with custom parameters
        var executionResult = await _sqlExecutionService.ExecuteSqlAsync(
            result.Sql, 
            result.Parameters);

        return executionResult.Results;
    }
}
```

### 9. Error Handling and Retry Logic

```csharp
public class ResilientSqlService
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly ISqlExecutionService _sqlExecutionService;
    private readonly ILogger<ResilientSqlService> _logger;
    private readonly RetryPolicy _retryPolicy;

    public ResilientSqlService(
        ISqlGenerator sqlGenerator,
        ISqlExecutionService sqlExecutionService,
        ILogger<ResilientSqlService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _sqlExecutionService = sqlExecutionService;
        _logger = logger;
        
        // Define retry policy for transient database errors
        _retryPolicy = Policy
            .Handle<SqlException>(ex => IsTransientError(ex))
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Database error on attempt {RetryCount}, retrying in {RetryTimeSpan}...",
                        retryCount,
                        timeSpan);
                });
    }

    public async Task<IEnumerable<T>> ExecuteQueryWithResilienceAsync<T>(
        string templateId, 
        Dictionary<string, object> parameters, 
        TenantContext tenantContext)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Generate SQL
            var result = await _sqlGenerator.GenerateSqlAsync(
                templateId, 
                parameters, 
                tenantContext);

            if (!result.IsSuccessful)
            {
                _logger.LogError("SQL generation failed: {ErrorMessage}", result.ErrorMessage);
                throw new ApplicationException($"Failed to generate SQL: {result.ErrorMessage}");
            }

            // Execute with timeout
            var executionResult = await _sqlExecutionService.ExecuteSqlWithTimeoutAsync<T>(
                result.Sql,
                result.Parameters,
                TimeSpan.FromSeconds(30)); // 30 second timeout

            return executionResult.Results;
        });
    }

    private bool IsTransientError(SqlException ex)
    {
        // List of SQL error codes that are considered transient
        int[] transientErrorCodes = { 
            -2, // Timeout
            53,  // Server not found
            1204, // Lock issue
            1205, // Deadlock victim
            1222, // Lock request timeout
            10053, // Transport error
            10054, // Connection reset
            10060, // Connection timeout
            40197, // Error processing request
            40501, // Service busy
            40613, // Database busy
            49918, // Not enough resources
            49919, // Not enough resources - reduce memory
            49920  // Not enough resources - reduce CPU
        };
        
        return transientErrorCodes.Contains(ex.Number);
    }
} 