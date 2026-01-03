using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Web.Services;

/// <summary>
/// A scoped service that holds the current Tenant for the request lifecycle.
/// populated by TenantResolutionMiddleware.
/// </summary>
public class TenantContext : ITenantProvider
{
    private Tenant? _currentTenant;

    public Tenant? CurrentTenant
    {
        get => _currentTenant;
        set => _currentTenant = value;
    }

    // Implementation of ITenantProvider for EF Core Global Query Filter
    // If no tenant is resolved (e.g. middleware hasn't run), return null or handle gracefully.
    // For EF Core filters, returning a default/empty GUID might be safer than null if the column is non-nullable, 
    // but our global query is `e.TenantId == _tenantProvider.TenantId`. 
    // If TenantId is missing/null, we should probably return Guid.Empty to return NO results (secure by default).
    public Guid? TenantId => _currentTenant?.Id ?? Guid.Empty; 
}
