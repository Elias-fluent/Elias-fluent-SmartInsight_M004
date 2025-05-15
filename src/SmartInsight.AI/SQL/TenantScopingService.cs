using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Implementation of ITenantScopingService that enforces tenant isolation in SQL queries
    /// </summary>
    public class TenantScopingService : ITenantScopingService
    {
        private readonly ILogger<TenantScopingService> _logger;
        private readonly Dictionary<string, string> _tenantColumnMappings;

        /// <summary>
        /// Creates a new instance of TenantScopingService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public TenantScopingService(ILogger<TenantScopingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Define mappings between table names and their tenant ID columns
            // This could be moved to configuration or database metadata in a real implementation
            _tenantColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Users", "TenantId" },
                { "DataSources", "TenantId" },
                { "Conversations", "TenantId" },
                { "ConversationHistory", "TenantId" },
                { "Documents", "TenantId" },
                { "Terms", "TenantId" },
                { "KnowledgeItems", "TenantId" },
                { "Settings", "TenantId" },
                // Add more table mappings as needed
            };
        }

        /// <inheritdoc />
        public async Task<string> ApplyTenantScopingAsync(
            string sql, 
            TenantContext tenantContext, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL query cannot be null or empty", nameof(sql));
            }

            if (tenantContext == null)
            {
                throw new ArgumentNullException(nameof(tenantContext));
            }

            _logger.LogDebug("Applying tenant scoping to SQL query for tenant {TenantId}", tenantContext.TenantId);

            try
            {
                // Parse the SQL to identify tables and apply tenant filters
                var scopedSql = ApplyTenantFilters(sql, tenantContext.TenantId);
                
                _logger.LogDebug("Successfully applied tenant scoping to SQL query");
                return await Task.FromResult(scopedSql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying tenant scoping to SQL query");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TenantContext> GetTenantContextAsync(
            Guid? tenantId = null, 
            Guid? userId = null, 
            CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would likely come from an HTTP context,
            // current user claims, or a database lookup. For now, we'll create a simple
            // context with the provided IDs or defaults
            
            var context = new TenantContext
            {
                TenantId = tenantId ?? Guid.Empty,
                UserId = userId ?? Guid.Empty,
                Permissions = new List<string>() // Would be populated from user/role data
            };

            return await Task.FromResult(context);
        }

        /// <inheritdoc />
        public async Task<bool> ValidateTenantAccessAsync(
            Guid userId, 
            Guid tenantId, 
            CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would check if the user has access to the tenant
            // For now, we'll just return true
            // This would typically query a user-tenant relationship table or check claims
            
            return await Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<bool> ValidateTenantIsolationAsync(
            string sql, 
            TenantContext tenantContext, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL query cannot be null or empty", nameof(sql));
            }

            if (tenantContext == null)
            {
                throw new ArgumentNullException(nameof(tenantContext));
            }

            try
            {
                // Check if all tables in the query have tenant filters applied
                var isIsolated = CheckTenantIsolation(sql, tenantContext.TenantId);
                return await Task.FromResult(isIsolated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tenant isolation for SQL query");
                return await Task.FromResult(false);
            }
        }

        #region Private Implementation Methods

        private string ApplyTenantFilters(string sql, Guid tenantId)
        {
            // Find table names in the SQL statement
            var tableNames = ExtractTableNames(sql);
            
            if (!tableNames.Any())
            {
                return sql; // No tables found, return original query
            }

            // SQL operations each need different tenant filter approaches
            if (IsSelectQuery(sql))
            {
                return ApplySelectTenantFilters(sql, tableNames, tenantId);
            }
            else if (IsUpdateQuery(sql))
            {
                return ApplyUpdateTenantFilters(sql, tableNames, tenantId);
            }
            else if (IsDeleteQuery(sql))
            {
                return ApplyDeleteTenantFilters(sql, tableNames, tenantId);
            }
            else if (IsInsertQuery(sql))
            {
                return ApplyInsertTenantFilters(sql, tableNames, tenantId);
            }
            
            // For other types of queries, return the original
            return sql;
        }

        private bool CheckTenantIsolation(string sql, Guid tenantId)
        {
            // Find table names in the SQL statement
            var tableNames = ExtractTableNames(sql);
            
            if (!tableNames.Any())
            {
                return true; // No tables to check, consider it isolated
            }

            foreach (var tableName in tableNames)
            {
                // Skip tables that don't have tenant columns
                if (!_tenantColumnMappings.TryGetValue(tableName, out var tenantColumn))
                {
                    continue;
                }

                // Check if the table has a tenant filter
                var tenantFilterPattern = $@"{tableName}(?:\s+(?:AS\s+)?[a-zA-Z0-9_]+)?\s+.*{tenantColumn}\s*=\s*[']?{tenantId}[']?";
                var whereClausePattern = $@"WHERE\s+(?:.*\s+AND\s+)?{tenantColumn}\s*=\s*[']?{tenantId}[']?";
                
                var hasTenantFilter = Regex.IsMatch(sql, tenantFilterPattern, RegexOptions.IgnoreCase) ||
                                      Regex.IsMatch(sql, whereClausePattern, RegexOptions.IgnoreCase);
                
                if (!hasTenantFilter)
                {
                    return false; // At least one table doesn't have tenant isolation
                }
            }
            
            return true; // All tables have tenant isolation
        }

        private IEnumerable<string> ExtractTableNames(string sql)
        {
            // This is a simplified approach to extract table names
            // A real implementation would use SQL parsing libraries
            
            // FROM table or JOIN patterns
            var fromPattern = @"FROM\s+([a-zA-Z0-9_]+)";
            var joinPattern = @"JOIN\s+([a-zA-Z0-9_]+)";
            var updatePattern = @"UPDATE\s+([a-zA-Z0-9_]+)";
            var insertPattern = @"INSERT\s+INTO\s+([a-zA-Z0-9_]+)";
            var deletePattern = @"DELETE\s+FROM\s+([a-zA-Z0-9_]+)";
            
            var tables = new List<string>();
            
            // Extract tables from FROM clause
            var fromMatches = Regex.Matches(sql, fromPattern, RegexOptions.IgnoreCase);
            foreach (Match match in fromMatches)
            {
                if (match.Groups.Count > 1)
                {
                    tables.Add(match.Groups[1].Value.Trim());
                }
            }
            
            // Extract tables from JOIN clauses
            var joinMatches = Regex.Matches(sql, joinPattern, RegexOptions.IgnoreCase);
            foreach (Match match in joinMatches)
            {
                if (match.Groups.Count > 1)
                {
                    tables.Add(match.Groups[1].Value.Trim());
                }
            }
            
            // Extract tables from UPDATE clause
            var updateMatches = Regex.Matches(sql, updatePattern, RegexOptions.IgnoreCase);
            foreach (Match match in updateMatches)
            {
                if (match.Groups.Count > 1)
                {
                    tables.Add(match.Groups[1].Value.Trim());
                }
            }
            
            // Extract tables from INSERT clause
            var insertMatches = Regex.Matches(sql, insertPattern, RegexOptions.IgnoreCase);
            foreach (Match match in insertMatches)
            {
                if (match.Groups.Count > 1)
                {
                    tables.Add(match.Groups[1].Value.Trim());
                }
            }
            
            // Extract tables from DELETE clause
            var deleteMatches = Regex.Matches(sql, deletePattern, RegexOptions.IgnoreCase);
            foreach (Match match in deleteMatches)
            {
                if (match.Groups.Count > 1)
                {
                    tables.Add(match.Groups[1].Value.Trim());
                }
            }
            
            return tables.Distinct();
        }

        private bool IsSelectQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*SELECT", RegexOptions.IgnoreCase);
        }

        private bool IsUpdateQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*UPDATE", RegexOptions.IgnoreCase);
        }

        private bool IsDeleteQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*DELETE", RegexOptions.IgnoreCase);
        }

        private bool IsInsertQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*INSERT", RegexOptions.IgnoreCase);
        }

        private string ApplySelectTenantFilters(string sql, IEnumerable<string> tableNames, Guid tenantId)
        {
            // For SELECT queries, we need to add tenant filters to the WHERE clause
            var modifiedSql = sql;
            
            foreach (var tableName in tableNames)
            {
                // Skip tables that don't have tenant columns
                if (!_tenantColumnMappings.TryGetValue(tableName, out var tenantColumn))
                {
                    continue;
                }
                
                // Check if the table has an alias
                var aliasPattern = $@"{tableName}\s+(?:AS\s+)?([a-zA-Z0-9_]+)";
                var aliasMatch = Regex.Match(sql, aliasPattern, RegexOptions.IgnoreCase);
                string tableReference = tableName;
                
                if (aliasMatch.Success && aliasMatch.Groups.Count > 1)
                {
                    tableReference = aliasMatch.Groups[1].Value.Trim();
                }
                
                // Check if the query already has a WHERE clause
                if (Regex.IsMatch(modifiedSql, @"\bWHERE\b", RegexOptions.IgnoreCase))
                {
                    // Add tenant filter to existing WHERE clause
                    modifiedSql = Regex.Replace(
                        modifiedSql,
                        @"WHERE\b",
                        $"WHERE {tableReference}.{tenantColumn} = '{tenantId}' AND ",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    // Find a good place to add WHERE clause (before ORDER BY, GROUP BY, HAVING, etc.)
                    var orderByMatch = Regex.Match(modifiedSql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase);
                    var groupByMatch = Regex.Match(modifiedSql, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase);
                    var havingMatch = Regex.Match(modifiedSql, @"\bHAVING\b", RegexOptions.IgnoreCase);
                    var limitMatch = Regex.Match(modifiedSql, @"\bLIMIT\b", RegexOptions.IgnoreCase);
                    
                    int insertPosition = modifiedSql.Length;
                    
                    if (orderByMatch.Success && orderByMatch.Index < insertPosition)
                    {
                        insertPosition = orderByMatch.Index;
                    }
                    
                    if (groupByMatch.Success && groupByMatch.Index < insertPosition)
                    {
                        insertPosition = groupByMatch.Index;
                    }
                    
                    if (havingMatch.Success && havingMatch.Index < insertPosition)
                    {
                        insertPosition = havingMatch.Index;
                    }
                    
                    if (limitMatch.Success && limitMatch.Index < insertPosition)
                    {
                        insertPosition = limitMatch.Index;
                    }
                    
                    // Add WHERE clause with tenant filter
                    modifiedSql = modifiedSql.Insert(insertPosition, $" WHERE {tableReference}.{tenantColumn} = '{tenantId}' ");
                }
            }
            
            return modifiedSql;
        }

        private string ApplyUpdateTenantFilters(string sql, IEnumerable<string> tableNames, Guid tenantId)
        {
            // For UPDATE queries, we need to add tenant filters to the WHERE clause
            var modifiedSql = sql;
            
            foreach (var tableName in tableNames)
            {
                // Skip tables that don't have tenant columns
                if (!_tenantColumnMappings.TryGetValue(tableName, out var tenantColumn))
                {
                    continue;
                }
                
                // Check if the query already has a WHERE clause
                if (Regex.IsMatch(modifiedSql, @"\bWHERE\b", RegexOptions.IgnoreCase))
                {
                    // Add tenant filter to existing WHERE clause
                    modifiedSql = Regex.Replace(
                        modifiedSql,
                        @"WHERE\b",
                        $"WHERE {tenantColumn} = '{tenantId}' AND ",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    // Add WHERE clause with tenant filter at the end
                    modifiedSql += $" WHERE {tenantColumn} = '{tenantId}'";
                }
            }
            
            return modifiedSql;
        }

        private string ApplyDeleteTenantFilters(string sql, IEnumerable<string> tableNames, Guid tenantId)
        {
            // For DELETE queries, we need to add tenant filters to the WHERE clause
            var modifiedSql = sql;
            
            foreach (var tableName in tableNames)
            {
                // Skip tables that don't have tenant columns
                if (!_tenantColumnMappings.TryGetValue(tableName, out var tenantColumn))
                {
                    continue;
                }
                
                // Check if the query already has a WHERE clause
                if (Regex.IsMatch(modifiedSql, @"\bWHERE\b", RegexOptions.IgnoreCase))
                {
                    // Add tenant filter to existing WHERE clause
                    modifiedSql = Regex.Replace(
                        modifiedSql,
                        @"WHERE\b",
                        $"WHERE {tenantColumn} = '{tenantId}' AND ",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    // Add WHERE clause with tenant filter at the end
                    modifiedSql += $" WHERE {tenantColumn} = '{tenantId}'";
                }
            }
            
            return modifiedSql;
        }

        private string ApplyInsertTenantFilters(string sql, IEnumerable<string> tableNames, Guid tenantId)
        {
            // For INSERT queries, we need to add the tenant ID to the list of columns/values
            var modifiedSql = sql;
            
            foreach (var tableName in tableNames)
            {
                // Skip tables that don't have tenant columns
                if (!_tenantColumnMappings.TryGetValue(tableName, out var tenantColumn))
                {
                    continue;
                }
                
                // Check if the tenant column is already included
                var columnPattern = $@"\(\s*{tenantColumn}\s*,";
                var hasColumn = Regex.IsMatch(modifiedSql, columnPattern, RegexOptions.IgnoreCase);
                
                if (!hasColumn)
                {
                    // Parse the INSERT statement to add tenant column
                    var insertPattern = @"INSERT\s+INTO\s+([^\s]+)\s*\(([^)]+)\)\s*VALUES\s*\(([^)]+)\)";
                    var insertMatch = Regex.Match(modifiedSql, insertPattern, RegexOptions.IgnoreCase);
                    
                    if (insertMatch.Success && insertMatch.Groups.Count > 3)
                    {
                        var columns = insertMatch.Groups[2].Value.Trim();
                        var values = insertMatch.Groups[3].Value.Trim();
                        
                        // Add tenant column and value
                        var newColumns = $"{columns}, {tenantColumn}";
                        var newValues = $"{values}, '{tenantId}'";
                        
                        modifiedSql = Regex.Replace(
                            modifiedSql,
                            insertPattern,
                            $"INSERT INTO $1 ({newColumns}) VALUES ({newValues})",
                            RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Handle INSERT without column list
                        var simpleInsertPattern = @"INSERT\s+INTO\s+([^\s]+)\s+VALUES\s*\(([^)]+)\)";
                        var simpleInsertMatch = Regex.Match(modifiedSql, simpleInsertPattern, RegexOptions.IgnoreCase);
                        
                        if (simpleInsertMatch.Success && simpleInsertMatch.Groups.Count > 2)
                        {
                            var values = simpleInsertMatch.Groups[2].Value.Trim();
                            
                            // Add tenant value
                            var newValues = $"{values}, '{tenantId}'";
                            
                            modifiedSql = Regex.Replace(
                                modifiedSql,
                                simpleInsertPattern,
                                $"INSERT INTO $1 VALUES ({newValues})",
                                RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            
            return modifiedSql;
        }

        #endregion
    }
} 