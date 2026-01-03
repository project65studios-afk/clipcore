using System.Text.Json;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClipCore.Infrastructure.Services;

/// <summary>
/// A scoped service that holds the current Tenant for the request lifecycle.
/// populated by TenantResolutionMiddleware.
/// </summary>
public class TenantContext : ITenantProvider
{
    private readonly ILogger<TenantContext> _logger;
    private Tenant? _currentTenant;
    private ThemeSettings? _cachedTheme;

    public TenantContext(ILogger<TenantContext> logger)
    {
        _logger = logger;
    }

    public Tenant? CurrentTenant
    {
        get => _currentTenant;
        set 
        {
            _currentTenant = value;
            _cachedTheme = null; // Reset cache when tenant changes
        }
    }

    public ThemeSettings Theme => _cachedTheme ??= ParseTheme();

    private ThemeSettings ParseTheme()
    {
        if (CurrentTenant != null && !string.IsNullOrEmpty(CurrentTenant.ThemeSettingsJson))
        {
            try
            {
                _cachedTheme = JsonSerializer.Deserialize<ThemeSettings>(CurrentTenant.ThemeSettingsJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                _cachedTheme = new ThemeSettings();
            }
        }
        return _cachedTheme ?? new ThemeSettings();
    }

    // Implementation of ITenantProvider for EF Core Global Query Filter
    public Guid? TenantId => _currentTenant?.Id ?? Guid.Empty; 
}
