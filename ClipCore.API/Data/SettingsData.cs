using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;

namespace ClipCore.API.Data;

public class SettingsData : ISettingsData
{
    private readonly ISqlDataAccess _db;
    public SettingsData(ISqlDataAccess db) => _db = db;

    public Task<string?> GetValueAsync(string key) =>
        _db.ExecuteScalar<string?, dynamic>(
            @"SELECT ""Value"" FROM ""Settings"" WHERE ""Key""=@Key",
            new { Key = key });

    public Task SetValueAsync(string key, string value) =>
        _db.SaveData("CALL cc_u_setting(@Key, @Value)", new { Key = key, Value = value });

    public Task<IEnumerable<Setting>> ListAllAsync() =>
        _db.LoadData<Setting>(@"SELECT * FROM ""Settings""");
}
