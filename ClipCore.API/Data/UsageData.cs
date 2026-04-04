using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;

namespace ClipCore.API.Data;

public class UsageData : IUsageData
{
    private readonly ISqlDataAccess _db;
    public UsageData(ISqlDataAccess db) => _db = db;

    public async Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date) =>
        await _db.LoadSingle<DailyWatchUsage, dynamic>(
            @"SELECT * FROM ""DailyWatchUsages"" WHERE ""IpAddress""=@IpAddress AND ""Date""=@Date",
            new { IpAddress = ipAddress, Date = date })
        ?? new DailyWatchUsage { IpAddress = ipAddress, Date = date };

    public Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null) =>
        _db.SaveData("CALL cc_u_usage_increment(@IpAddress, @Date, @UserId)",
            new { IpAddress = ipAddress, Date = date, UserId = userId });
}
