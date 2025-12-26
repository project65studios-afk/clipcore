using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _context;

    public UsageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date)
    {
        return await _context.DailyWatchUsages
            .FirstOrDefaultAsync(u => u.IpAddress == ipAddress && u.Date == date) 
            ?? new DailyWatchUsage { IpAddress = ipAddress, Date = date };
    }

    public async Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null)
    {
        var usage = await _context.DailyWatchUsages
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
            _context.DailyWatchUsages.Add(usage);
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

        await _context.SaveChangesAsync();
    }
}
