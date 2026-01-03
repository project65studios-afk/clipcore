using ClipCore.Core.Interfaces;

namespace ClipCore.Web.Services;

public class StoreSettingsService : IDisposable
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly GlobalSettingsNotifier _globalNotifier;
    private string? _storeName;
    private string? _brandLogoUrl;

    public event Action? OnChange;

    public StoreSettingsService(ISettingsRepository settingsRepository, GlobalSettingsNotifier globalNotifier)
    {
        _settingsRepository = settingsRepository;
        _globalNotifier = globalNotifier;
        _globalNotifier.OnSettingsChanged += HandleGlobalUpdate;
    }

    public async Task<string> GetStoreNameAsync()
    {
        if (_storeName == null)
        {
            _storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "ClipCore Studios";
        }
        return _storeName;
    }

    public async Task<string?> GetBrandLogoUrlAsync()
    {
        if (_brandLogoUrl == null)
        {
            _brandLogoUrl = await _settingsRepository.GetValueAsync("BrandLogoUrl");
        }
        return _brandLogoUrl;
    }

    private void HandleGlobalUpdate(string key, string value)
    {
        if (key == "StoreName")
        {
            _storeName = value;
        }
        else if (key == "BrandLogoUrl")
        {
            _brandLogoUrl = value;
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
