using ClipCore.Core.Entities;

namespace ClipCore.API.Interfaces;

public interface ISettingsData
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<IEnumerable<Setting>> ListAllAsync();
}
