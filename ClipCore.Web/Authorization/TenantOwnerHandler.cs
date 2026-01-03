using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace ClipCore.Web.Authorization;

public class TenantOwnerHandler : AuthorizationHandler<TenantOwnerRequirement>
{
    private readonly TenantContext _tenantContext;
    private readonly ITenantAuthorizationService _authService;
    private readonly ILogger<TenantOwnerHandler> _logger;

    public TenantOwnerHandler(
        TenantContext tenantContext, 
        ITenantAuthorizationService authService,
        ILogger<TenantOwnerHandler> logger)
    {
        _tenantContext = tenantContext;
        _authService = authService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantOwnerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Must have the global Admin role 
        if (!context.User.IsInRole("Admin"))
        {
            return;
        }

        if (_tenantContext.CurrentTenant == null)
        {
            return;
        }

        var isOwner = await _authService.IsOwnerAsync(context.User, _tenantContext.CurrentTenant.Id);
        
        // Super Admin bypass for Owners page? Usually yes, to help sellers.
        var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (email == "admin@clipcore.com") isOwner = true;

        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}
