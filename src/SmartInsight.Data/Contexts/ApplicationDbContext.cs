using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data.Common;
using System.Reflection;
using System.Linq.Expressions;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Configurations;

namespace SmartInsight.Data.Contexts;

/// <summary>
/// Main application database context for Entity Framework Core
/// </summary>
public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser, ApplicationRole, Guid,
    IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Legacy Users in the system
    /// </summary>
    public DbSet<User> LegacyUsers { get; set; }
    
    /// <summary>
    /// Tenants in the system
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; }
    
    /// <summary>
    /// Knowledge nodes representing pieces of information
    /// </summary>
    public DbSet<KnowledgeNode> KnowledgeNodes { get; set; }
    
    /// <summary>
    /// Relations between knowledge nodes
    /// </summary>
    public DbSet<Relation> Relations { get; set; }
    
    /// <summary>
    /// Documents in the system
    /// </summary>
    public DbSet<Document> Documents { get; set; }
    
    /// <summary>
    /// Data sources for information ingestion
    /// </summary>
    public DbSet<DataSource> DataSources { get; set; }
    
    /// <summary>
    /// Terms extracted from knowledge nodes
    /// </summary>
    public DbSet<Term> Terms { get; set; }
    
    /// <summary>
    /// Vector indices for semantic search
    /// </summary>
    public DbSet<VectorIndex> VectorIndices { get; set; }
    
    /// <summary>
    /// Logs of user conversations with the system
    /// </summary>
    public DbSet<ConversationLog> ConversationLogs { get; set; }
    
    /// <summary>
    /// Metrics logs for system performance
    /// </summary>
    public DbSet<MetricsLog> MetricsLogs { get; set; }
    
    /// <summary>
    /// Secure credentials storage
    /// </summary>
    public DbSet<Credential> Credentials { get; set; }
    
    /// <summary>
    /// Data ingestion job definitions
    /// </summary>
    public DbSet<IngestionJobEntity> IngestionJobs { get; set; }

    /// <summary>
    /// Constructor that accepts DbContextOptions
    /// </summary>
    /// <param name="options">DbContext configuration options</param>
    /// <param name="tenantAccessor">Optional tenant accessor for multi-tenant filtering</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantAccessor? tenantAccessor = null) 
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Configures additional options for the DbContext
    /// </summary>
    /// <param name="optionsBuilder">Options builder for configuration</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Register events to set the tenant ID on the connection when it's established
        if (optionsBuilder.IsConfigured && _tenantAccessor != null)
        {
            optionsBuilder.UseNpgsql().AddInterceptors(new NpgsqlConnectionInterceptor(this));
        }
    }

    /// <summary>
    /// Configures the model using the Fluent API
    /// </summary>
    /// <param name="modelBuilder">Model builder for configuration</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base implementation first to set up Identity tables
        base.OnModelCreating(modelBuilder);
        
        // Configure Identity tables
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        
        // Apply all entity configurations from the assembly
        // This will automatically find all classes that implement IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Configure soft delete filter for all entities inheriting from BaseEntity
        ConfigureSoftDeleteFilter(modelBuilder);
        
        // Set default schema
        modelBuilder.HasDefaultSchema("smartinsight");
        
        // Configure PostgreSQL-specific features
        Configurations.PostgresExtensions.ConfigurePostgresFeatures(modelBuilder);
        
        // Configure global query filters for multi-tenant data isolation
        ConfigureTenantFilters(modelBuilder);
        
        // Add filter to ApplicationUser to handle soft delete
        ConfigureIdentityFilters(modelBuilder);
    }
    
    /// <summary>
    /// Configures global query filters for soft-deleted entities
    /// </summary>
    /// <param name="modelBuilder">Model builder for configuration</param>
    private void ConfigureSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        // Find all entity types that derive from BaseEntity
        var softDeleteEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));
        
        foreach (var entityType in softDeleteEntityTypes)
        {
            // Skip if the entity is owned or has no IsDeleted property
            if (entityType.IsOwned() || !entityType.ClrType.GetProperties().Any(p => p.Name == nameof(BaseEntity.IsDeleted)))
                continue;
                
            // Create parameter expression for the IsDeleted property
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var prop = Expression.PropertyOrField(parameter, nameof(BaseEntity.IsDeleted));
            var notDeleted = Expression.Not(prop);
            var lambda = Expression.Lambda(notDeleted, parameter);
            
            // Apply filter to the entity type
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
    
    /// <summary>
    /// Configures global query filters for multi-tenant entities
    /// </summary>
    /// <param name="modelBuilder">Model builder for configuration</param>
    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        // Find all entity types that derive from BaseMultiTenantEntity
        var multiTenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(BaseMultiTenantEntity).IsAssignableFrom(t.ClrType));
            
        foreach (var entityType in multiTenantEntityTypes)
        {
            // Skip if the entity is owned
            if (entityType.IsOwned())
                continue;
                
            // Create the filter expression:
            // e => !_tenantAccessor.IsMultiTenantContext() || 
            //      _tenantAccessor.CanAccessAllTenants() ||
            //      e.TenantId == _tenantAccessor.GetCurrentTenantId()
            
            var entityClrType = entityType.ClrType;
            var parameter = Expression.Parameter(entityClrType, "e");
            
            // Create a closure to capture the _tenantAccessor instance
            var tenantAccessorInstanceField = GetType().GetField("_tenantAccessor", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            var tenantAccessorInstance = Expression.Field(
                Expression.Constant(this), tenantAccessorInstanceField!);
                
            // First condition: !_tenantAccessor.IsMultiTenantContext()
            var isMultiTenantContextMethod = typeof(ITenantAccessor).GetMethod("IsMultiTenantContext");
            var isMultiTenantContextCall = Expression.Call(tenantAccessorInstance, isMultiTenantContextMethod!);
            var notInMultiTenantContext = Expression.Not(isMultiTenantContextCall);
            
            // Second condition: _tenantAccessor.CanAccessAllTenants()
            var canAccessAllTenantsMethod = typeof(ITenantAccessor).GetMethod("CanAccessAllTenants");
            var canAccessAllTenantsCall = Expression.Call(tenantAccessorInstance, canAccessAllTenantsMethod!);
            
            // Third condition: e.TenantId == _tenantAccessor.GetCurrentTenantId()
            var tenantIdProperty = Expression.PropertyOrField(parameter, nameof(BaseMultiTenantEntity.TenantId));
            var getCurrentTenantIdMethod = typeof(ITenantAccessor).GetMethod("GetCurrentTenantId");
            var getCurrentTenantIdCall = Expression.Call(tenantAccessorInstance, getCurrentTenantIdMethod!);
            var tenantIdEquals = Expression.Equal(tenantIdProperty, getCurrentTenantIdCall);
            
            // Combine conditions with OR
            var orExpression = Expression.OrElse(
                Expression.OrElse(notInMultiTenantContext, canAccessAllTenantsCall),
                tenantIdEquals
            );
            
            // Create lambda and apply filter
            var lambda = Expression.Lambda(orExpression, parameter);
            modelBuilder.Entity(entityClrType).HasQueryFilter(lambda);
        }
    }
    
    /// <summary>
    /// Overrides SaveChanges to handle tenant ID assignment for multi-tenant entities
    /// </summary>
    public override int SaveChanges()
    {
        SetTenantIdForNewEntities();
        return base.SaveChanges();
    }
    
    /// <summary>
    /// Overrides SaveChangesAsync to handle tenant ID assignment for multi-tenant entities
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdForNewEntities();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Sets tenant ID for new multi-tenant entities before saving
    /// </summary>
    private void SetTenantIdForNewEntities()
    {
        if (_tenantAccessor == null || !_tenantAccessor.IsMultiTenantContext())
            return;
            
        var currentTenantId = _tenantAccessor.GetCurrentTenantId();
        if (!currentTenantId.HasValue)
            return;
            
        // Get all added entities that are multi-tenant
        var newMultiTenantEntities = ChangeTracker.Entries<BaseMultiTenantEntity>()
            .Where(e => e.State == EntityState.Added)
            .Where(e => e.Entity.TenantId == Guid.Empty) // Only set tenant ID if not already set
            .Select(e => e.Entity);
            
        foreach (var entity in newMultiTenantEntities)
        {
            entity.TenantId = currentTenantId.Value;
        }
        
        // Handle ApplicationUser and ApplicationRole entities
        var newUsers = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State == EntityState.Added)
            .Where(e => e.Entity.PrimaryTenantId == Guid.Empty) // Only set tenant ID if not already set
            .Select(e => e.Entity);
            
        foreach (var user in newUsers)
        {
            user.PrimaryTenantId = currentTenantId.Value;
        }
        
        var newRoles = ChangeTracker.Entries<ApplicationRole>()
            .Where(e => e.State == EntityState.Added)
            .Where(e => e.Entity.TenantId == null || e.Entity.TenantId == Guid.Empty) // Only set tenant ID if not already set
            .Select(e => e.Entity);
            
        foreach (var role in newRoles)
        {
            role.TenantId = currentTenantId.Value;
        }
    }
    
    /// <summary>
    /// Sets the current tenant ID in the PostgreSQL connection for Row-Level Security
    /// </summary>
    /// <param name="connection">The database connection</param>
    public void SetCurrentTenantForConnection(DbConnection connection)
    {
        if (connection is NpgsqlConnection npgsqlConnection && _tenantAccessor != null)
        {
            var currentTenantId = _tenantAccessor.GetCurrentTenantId();
            
            // Set the current tenant ID as a session variable for the PostgreSQL connection
            if (currentTenantId.HasValue)
            {
                // Ensure connection is open
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                // Execute a command to set the app.current_tenant session variable
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.current_tenant = '{currentTenantId}'";
                cmd.ExecuteNonQuery();
            }
            else if (_tenantAccessor.CanAccessAllTenants())
            {
                // For admin users who can access all tenants, use a special value
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SET app.current_tenant = '00000000-0000-0000-0000-000000000000'";
                cmd.ExecuteNonQuery();
            }
        }
    }
    
    /// <summary>
    /// Configures filters for Identity entities (ApplicationUser, ApplicationRole)
    /// </summary>
    /// <param name="modelBuilder">Model builder for configuration</param>
    private void ConfigureIdentityFilters(ModelBuilder modelBuilder)
    {
        // Configure soft delete filter for ApplicationUser
        modelBuilder.Entity<ApplicationUser>()
            .HasQueryFilter(u => !u.IsDeleted);
        
        // Configure tenant filter for ApplicationRole
        if (_tenantAccessor != null)
        {
            // Create parameter expression for the ApplicationRole
            var roleParameter = Expression.Parameter(typeof(ApplicationRole), "r");
            
            // Create a closure to capture the _tenantAccessor instance
            var tenantAccessorInstanceField = GetType().GetField("_tenantAccessor", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            var tenantAccessorInstance = Expression.Field(
                Expression.Constant(this), tenantAccessorInstanceField!);
            
            // First condition: Role has no tenant ID (global role)
            var tenantIdProperty = Expression.PropertyOrField(roleParameter, nameof(ApplicationRole.TenantId));
            var tenantIdIsNull = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(Guid?)));
            
            // Second condition: !_tenantAccessor.IsMultiTenantContext()
            var isMultiTenantContextMethod = typeof(ITenantAccessor).GetMethod("IsMultiTenantContext");
            var isMultiTenantContextCall = Expression.Call(tenantAccessorInstance, isMultiTenantContextMethod!);
            var notInMultiTenantContext = Expression.Not(isMultiTenantContextCall);
            
            // Third condition: _tenantAccessor.CanAccessAllTenants()
            var canAccessAllTenantsMethod = typeof(ITenantAccessor).GetMethod("CanAccessAllTenants");
            var canAccessAllTenantsCall = Expression.Call(tenantAccessorInstance, canAccessAllTenantsMethod!);
            
            // Fourth condition: r.TenantId == _tenantAccessor.GetCurrentTenantId()
            var getCurrentTenantIdMethod = typeof(ITenantAccessor).GetMethod("GetCurrentTenantId");
            var getCurrentTenantIdCall = Expression.Call(tenantAccessorInstance, getCurrentTenantIdMethod!);
            var tenantIdValueProperty = Expression.Property(tenantIdProperty, "Value");
            var tenantIdEquals = Expression.Equal(tenantIdValueProperty, getCurrentTenantIdCall);
            
            // Check if tenant ID is not null before comparing values
            var hasValue = Expression.Property(tenantIdProperty, "HasValue");
            var tenantIdEqualsIfHasValue = Expression.Condition(
                hasValue,
                tenantIdEquals,
                Expression.Constant(false)
            );
            
            // Combine conditions with OR
            var combinedExpression = Expression.OrElse(
                tenantIdIsNull,
                Expression.OrElse(
                    Expression.OrElse(notInMultiTenantContext, canAccessAllTenantsCall),
                    tenantIdEqualsIfHasValue
                )
            );
            
            // Create lambda and apply filter
            var lambda = Expression.Lambda(combinedExpression, roleParameter);
            modelBuilder.Entity<ApplicationRole>().HasQueryFilter(lambda);
        }
    }
} 