#!/bin/bash
# This script demonstrates setting PostgreSQL session parameters for Row-Level Security

# Set the current user context
function set_user_context() {
    local user_id=$1
    local tenant_id=$2
    
    echo "-- Setting user context for RLS:"
    echo "SET app.current_user_id = '$user_id';"
    echo "SET app.current_tenant_id = '$tenant_id';"
}

# Reset the user context (clear RLS session parameters)
function reset_user_context() {
    echo "-- Resetting user context:"
    echo "RESET app.current_user_id;"
    echo "RESET app.current_tenant_id;"
}

# Example usage for application
echo "/*"
echo "Row-Level Security (RLS) User Context Management"
echo ""
echo "In your application code, you should set these session parameters"
echo "after a user has been authenticated and before executing any queries:"
echo ""
echo "1. First, retrieve the user's ID and their current selected tenant ID"
echo "2. Set the session parameters using SET commands"
echo "3. All subsequent queries will be automatically filtered by the RLS policies"
echo ""
echo "Example PostgreSQL commands:"
echo "*/

set_user_context "00000000-0000-0000-0000-000000000001" "00000000-0000-0000-0000-000000000001"
echo ""
echo "-- Now all queries will be filtered by tenant"
echo "SELECT * FROM app.documents;"
echo ""

reset_user_context
echo ""
echo "-- After resetting, access may be denied or unrestricted depending on default policy"
echo ""

echo "/*"
echo "Implementation notes for .NET application:"
echo ""
echo "1. Store the user_id and tenant_id in the ClaimsPrincipal after authentication"
echo "2. In your DbContext OnConfiguring method, set these parameters on the connection:"
echo "   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)"
echo "   {"
echo "       var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue('user_id');"
echo "       var tenantId = _httpContextAccessor.HttpContext?.User.FindFirstValue('tenant_id');"
echo ""
echo "       if (userId != null && tenantId != null)"
echo "       {"
echo "           optionsBuilder.UseNpgsql(_connectionString, options => {"
echo "               options.CommandTimeout(30);"
echo "               options.SetPostgresVersion(new Version(15, 0));"
echo "           });"
echo ""
echo "           optionsBuilder.AddInterceptors(new TenantDbConnectionInterceptor(userId, tenantId));"
echo "       }"
echo "   }"
echo ""
echo "3. Create a DbConnectionInterceptor to set the parameters on each connection:"
echo "   public class TenantDbConnectionInterceptor : DbConnectionInterceptor"
echo "   {"
echo "       private readonly string _userId;"
echo "       private readonly string _tenantId;"
echo ""
echo "       public TenantDbConnectionInterceptor(string userId, string tenantId)"
echo "       {"
echo "           _userId = userId;"
echo "           _tenantId = tenantId;"
echo "       }"
echo ""
echo "       public override async ValueTask<InterceptionResult> ConnectionOpeningAsync("
echo "           DbConnection connection,"
echo "           ConnectionEventData eventData,"
echo "           InterceptionResult result,"
echo "           CancellationToken cancellationToken = default)"
echo "       {"
echo "           // Set the session variables for RLS"
echo "           using (var cmd = connection.CreateCommand())"
echo "           {"
echo "               cmd.CommandText = $\"SET app.current_user_id = '{_userId}'; SET app.current_tenant_id = '{_tenantId}';\";"
echo "               await connection.OpenAsync(cancellationToken);"
echo "               await cmd.ExecuteNonQueryAsync(cancellationToken);"
echo "           }"
echo "           return result;"
echo "       }"
echo "   }"
echo "*/" 