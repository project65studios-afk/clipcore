using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SettingsRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.Settings.FindAsync(key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
            await context.Settings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            context.Settings.Update(setting);
        }
        await context.SaveChangesAsync();
    }

    public async Task<List<Setting>> ListAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Settings.ToListAsync();
    }
}
