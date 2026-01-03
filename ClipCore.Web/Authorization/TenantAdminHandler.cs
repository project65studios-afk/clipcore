using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace ClipCore.Web.Authorization;

public class TenantAdminHandler : AuthorizationHandler<TenantAdminRequirement>
{
    private readonly TenantContext _tenantContext;
    private readonly ITenantAuthorizationService _authService;
    private readonly ILogger<TenantAdminHandler> _logger;

    public TenantAdminHandler(
        TenantContext tenantContext, 
        ITenantAuthorizationService authService,
        ILogger<TenantAdminHandler> logger)
    {
        _tenantContext = tenantContext;
        _authService = authService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Must have the global Admin role (as a base check)
        if (!context.User.IsInRole("Admin"))
        {
            _logger.LogWarning("User {User} does NOT have the global 'Admin' role.", context.User.Identity.Name);
            return;
        }

        if (_tenantContext.CurrentTenant == null)
        {
            _logger.LogWarning("Authorization failed: No tenant resolved for the request.");
            return;
        }

        var isAdmin = await _authService.IsAdminAsync(context.User, _tenantContext.CurrentTenant.Id);
        
        if (isAdmin)
        {
            _logger.LogInformation("Authorization SUCCESS: User {User} is admin for tenant {Tenant}.", context.User.Identity.Name, _tenantContext.CurrentTenant.Name);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Authorization FAILED: User {User} is NOT an admin for tenant {Tenant}.", context.User.Identity.Name, _tenantContext.CurrentTenant.Name);
        }
    }
}
