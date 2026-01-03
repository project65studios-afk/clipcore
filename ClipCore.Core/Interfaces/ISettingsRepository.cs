using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<List<Setting>> ListAllAsync();
}
