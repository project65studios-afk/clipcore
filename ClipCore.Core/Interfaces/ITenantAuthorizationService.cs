using System.Security.Claims;

namespace ClipCore.Core.Interfaces;

public interface ITenantAuthorizationService
{
    Task<bool> IsAdminAsync(ClaimsPrincipal user, Guid tenantId);
    Task<bool> IsOwnerAsync(ClaimsPrincipal user, Guid tenantId);
}
