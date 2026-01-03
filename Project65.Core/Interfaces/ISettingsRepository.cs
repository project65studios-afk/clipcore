using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<List<Setting>> ListAllAsync();
}
