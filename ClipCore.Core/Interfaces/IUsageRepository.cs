using System;
using System.Threading.Tasks;
using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface IUsageRepository
{
    Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date);
    Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null);
}
