using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class ClipRepository : IClipRepository
{
    private readonly AppDbContext _context;

    public ClipRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Clip?> GetByIdAsync(string id)
    {
        return await _context.Clips
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Clip>> SearchAsync(string query)
    {
        return await _context.Clips
            .AsNoTracking()
            .Include(c => c.Event)
            .Where(c => c.Title.ToLower().Contains(query.ToLower()) || c.TagsJson.ToLower().Contains(query.ToLower()))
            .OrderByDescending(c => c.RecordingStartedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Clip clip)
    {
        await _context.Clips.AddAsync(clip);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Clip clip)
    {
        _context.Entry(clip).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}
