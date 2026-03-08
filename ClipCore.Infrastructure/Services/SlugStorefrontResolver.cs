using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClipCore.Infrastructure.Services;

/// <summary>
/// MVP implementation: resolves Storefront by /store/{slug} path segment.
/// Phase 2: swap this for a subdomain-based implementation.
/// </summary>
public class SlugStorefrontResolver : IStorefrontResolver
{
    private readonly IStorefrontRepository _storefrontRepository;

    public SlugStorefrontResolver(IStorefrontRepository storefrontRepository)
    {
        _storefrontRepository = storefrontRepository;
    }

    public async Task<Storefront?> ResolveAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        // Expects paths like /store/{slug} or /store/{slug}/anything
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0].Equals("store", StringComparison.OrdinalIgnoreCase))
        {
            var slug = segments[1];
            return await _storefrontRepository.GetBySlugAsync(slug);
        }
        return null;
    }
}
