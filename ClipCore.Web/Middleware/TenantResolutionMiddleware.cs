using ClipCore.Core.Entities;
using ClipCore.Infrastructure.Data;
using ClipCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ClipCore.Web.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, TenantContext tenantContext)
    {
        // 1. Get Host
        var host = context.Request.Host.Host;
        var path = context.Request.Path.Value?.ToLower();

        // Skip static files and assets to prevent DB lookups
        if (path != null && (path.Contains(".") || path.StartsWith("/_framework") || path.StartsWith("/_blazor")))
        {
            // Simple check for extensions (js, css, png, etc) roughly
            if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".ico") || path.EndsWith(".woff2"))
            {
                await _next(context);
                return;
            }
        }
        
        // 2. Determine Subdomain
        // Logic: if host is "carlos.clipcore.com", subdomain is "carlos".
        // If host is "clipcore.com" or "www.clipcore.com", it's the Platform (System).
        // For Localhost: "carlos.clipcore.local" -> "carlos". "clipcore.local" -> Platform.
        
        string? subdomain = null;
        var parts = host.Split('.');
        
        // Basic check for localhost vs production domains
        // This logic can be made more robust with configuration (e.g. "BaseDomain" setting)
        // For now, we assume the LAST two parts are the domain (e.g. clipcore.com, localhost:port is tricky)
        
        // Handling "localhost" without TLD
        if (host.Contains("localhost"))
        {
             // Localhost Logic: 
             // localhost:5000 -> Platform
             // carlos.localhost:5000 -> Tenant
             if (parts.Length > 1 && parts[0] != "localhost" && parts[0] != "www")
             {
                 subdomain = parts[0];
             }
        }
        else
        {
            // Production Logic (Assuming 2-part TLD like .com. If .co.uk, needs config)
            // ex: carlos.clipcore.com (3 parts) -> carlos
            // ex: clipcore.com (2 parts) -> null (Platform)
            if (parts.Length >= 3)
            {
                // Exclude www
                if (parts[0] != "www")
                {
                    subdomain = parts[0];
                }
            }
        }

        Tenant? tenant = null;

        if (!string.IsNullOrEmpty(subdomain))
        {
            _logger.LogInformation($"[TenantResolution] Lookup for subdomain: '{subdomain}'");
            // 3. Lookup Tenant
            // We use IgnoreQueryFilters because we haven't set the TenantId yet!
            tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain);
                
            if (tenant != null)
                 _logger.LogInformation($"[TenantResolution] Found Tenant: {tenant.Name} ({tenant.Id})");
            else
                 _logger.LogWarning($"[TenantResolution] Tenant not found for subdomain: '{subdomain}'");
        }

        if (tenant == null)
        {
            // 4. Fallback / Platform Handling
            if (!string.IsNullOrEmpty(subdomain))
            {
                 context.Response.StatusCode = 404;
                 await context.Response.WriteAsync($"Store '{subdomain}' not found");
                 return;
            }
        }

        // 5. Set Context
        if (tenant != null)
        {
            tenantContext.CurrentTenant = tenant;
        }

        await _next(context);
    }
}
