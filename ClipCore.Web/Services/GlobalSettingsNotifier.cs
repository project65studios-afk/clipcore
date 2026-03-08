namespace ClipCore.Web.Services;

public class GlobalSettingsNotifier
{
    public event Action<string, string>? OnSettingsChanged;

    public void NotifyUpdate(string key, string value)
    {
        OnSettingsChanged?.Invoke(key, value);
    }
}
