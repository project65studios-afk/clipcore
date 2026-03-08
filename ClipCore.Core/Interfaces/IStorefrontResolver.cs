using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace ClipCore.Core.Interfaces;

/// <summary>
/// Resolves the active Storefront from the current HTTP request.
/// MVP: slug-based (/store/{slug}). Phase 2: subdomain-based.
/// </summary>
public interface IStorefrontResolver
{
    Task<Storefront?> ResolveAsync(HttpContext context);
}
