using ClipCore.Core.Entities;

namespace ClipCore.API.Interfaces;

public interface IUsageData
{
    Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date);
    Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null);
}
