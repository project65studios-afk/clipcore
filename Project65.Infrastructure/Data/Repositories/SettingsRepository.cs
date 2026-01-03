using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _context;

    public SettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value)
    {
        var setting = await _context.Settings.FindAsync(key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
            await _context.Settings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            _context.Settings.Update(setting);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<Setting>> ListAllAsync()
    {
        return await _context.Settings.ToListAsync();
    }
}
