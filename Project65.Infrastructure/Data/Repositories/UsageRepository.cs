using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UsageRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DailyWatchUsages
            .FirstOrDefaultAsync(u => u.IpAddress == ipAddress && u.Date == date) 
            ?? new DailyWatchUsage { IpAddress = ipAddress, Date = date };
    }

    public async Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var usage = await context.DailyWatchUsages
            .FirstOrDefaultAsync(u => u.IpAddress == ipAddress && u.Date == date);

        if (usage == null)
        {
            usage = new DailyWatchUsage
            {
                IpAddress = ipAddress,
                Date = date,
                UserId = userId,
                TokenRequestCount = 1
            };
            context.DailyWatchUsages.Add(usage);
        }
        else
        {
            usage.TokenRequestCount++;
            // Update UserID if formerly anonymous but now logged in
            if (string.IsNullOrEmpty(usage.UserId) && !string.IsNullOrEmpty(userId))
            {
                usage.UserId = userId;
            }
        }

        await context.SaveChangesAsync();
    }
}
