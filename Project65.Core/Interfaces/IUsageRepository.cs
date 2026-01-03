using System;
using System.Threading.Tasks;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IUsageRepository
{
    Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date);
    Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null);
}
