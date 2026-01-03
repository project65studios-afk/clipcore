using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Services;

namespace ClipCore.Web.Services;

public class StoreSettingsService : IDisposable
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly GlobalSettingsNotifier _globalNotifier;
    private readonly TenantContext _tenantContext;
    private string? _storeName;
    private string? _brandLogoUrl;

    public event Action? OnChange;

    public StoreSettingsService(
        ISettingsRepository settingsRepository, 
        GlobalSettingsNotifier globalNotifier,
        TenantContext tenantContext)
    {
        _settingsRepository = settingsRepository;
        _globalNotifier = globalNotifier;
        _tenantContext = tenantContext;
        _globalNotifier.OnSettingsChanged += HandleGlobalUpdate;
    }

    public async Task<string> GetStoreNameAsync()
    {
        // Prioritize Tenant Name from entity
        if (_tenantContext.CurrentTenant != null)
        {
            return _tenantContext.CurrentTenant.Name;
        }

        if (_storeName == null)
        {
            _storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "ClipCore Studios";
        }
        return _storeName;
    }

    public async Task<string?> GetBrandLogoUrlAsync()
    {
        // Prioritize Tenant Logo from ThemeSettings
        if (_tenantContext.CurrentTenant != null)
        {
            return _tenantContext.Theme.LogoUrl;
        }

        if (_brandLogoUrl == null)
        {
            _brandLogoUrl = await _settingsRepository.GetValueAsync("BrandLogoUrl");
        }
        return _brandLogoUrl;
    }

    public async Task<string?> GetWatermarkUrlAsync()
    {
        // Prioritize Tenant Watermark from ThemeSettings
        if (_tenantContext.CurrentTenant != null)
        {
            return _tenantContext.Theme.WatermarkUrl;
        }

        return await _settingsRepository.GetValueAsync("WatermarkUrl");
    }

    private void HandleGlobalUpdate(string key, string value)
    {
        if (key == "StoreName")
        {
            _storeName = value;
        }
        else if (key == "BrandLogoUrl" || key == "ThemeUpdate")
        {
            _brandLogoUrl = null; // Force reload from context/settings
        }
        
        // We notify for ANY key so that DynamicTheme.razor (and others) can reload if needed
        NotifyStateChanged();
    }

    public void NotifySettingChanged(string key, string value)
    {
        // This notifies the global bridge, which then circles back to all StoreSettingsService instances
        _globalNotifier.NotifyUpdate(key, value);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _globalNotifier.OnSettingsChanged -= HandleGlobalUpdate;
    }
}
