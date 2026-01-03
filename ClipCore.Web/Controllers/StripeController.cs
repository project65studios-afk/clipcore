using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.Web.Controllers;

[Route("api/stripe")]
[ApiController]
public class StripeController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        IPaymentService paymentService,
        ITenantRepository tenantRepository,
        TenantContext tenantContext,
        ILogger<StripeController> logger)
    {
        _paymentService = paymentService;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet("connect/callback")]
    public async Task<IActionResult> ConnectCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError("Stripe Connect Error: {Error}", error);
            return Redirect("/admin/settings?error=stripe_connect_failed");
        }

        if (!Guid.TryParse(state, out var targetTenantId))
        {
            return BadRequest("Invalid state parameter.");
        }

        try
        {
            // Fetch the specific tenant (might be different from current if redirected from another subdomain)
            var tenant = await _tenantRepository.GetByIdAsync(targetTenantId);
            if (tenant == null)
            {
                return BadRequest("Tenant not found.");
            }

            // Exchange Code for Account ID
            var accountId = await _paymentService.OnboardTenantAsync(code);

            // Update Tenant record
            tenant.StripeConnectAccountId = accountId;
            await _tenantRepository.UpdateAsync(tenant);
            
            _logger.LogInformation("Stripe Connect Account Linked for Tenant {Tenant}: {AccountId}", tenant.Name, accountId);

            // Determine the return URL based on the subdomain
            // We want to redirect back to the tenant's specific subdomain on the same base domain
            var host = Request.Host.Value ?? throw new InvalidOperationException("Host header is missing."); // e.g., clipcore.test:5094 or project65.clipcore.test:5094
            
            // Logic: Find the "base" domain (e.g., clipcore.test:5094) 
            // and prefix it with the tenant's subdomain.
            string baseDomain = host;
            if (host.Contains(".clipcore.test")) 
            {
                // If we are on a subdomain like project65.clipcore.test, strip the first part
                var parts = host.Split('.');
                if (parts.Length > 2) {
                    baseDomain = string.Join(".", parts.Skip(1));
                }
            }
            // else: if it's already just clipcore.test:5094, baseDomain is correct
            
            var returnUrl = $"{Request.Scheme}://{tenant.Subdomain}.{baseDomain}/admin/settings?success=stripe_connected";
            
            return Redirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Stripe Connect onboarding for tenant {TenantId}", state);
            return Redirect("/admin/settings?error=onboarding_exception");
        }
    }
}
