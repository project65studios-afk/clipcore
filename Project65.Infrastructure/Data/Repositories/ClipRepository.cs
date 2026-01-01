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
            .AsNoTracking()
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

    public async Task<List<Clip>> GetByEventIdAsync(string eventId)
    {
        return await _context.Clips
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.RecordingStartedAt)
            .ToListAsync();
    }

    public async Task<List<Clip>> GetRelatedAsync(string eventId, string[] tags, string excludeClipId, int count = 4)
    {
        // 1. Get other clips from same event
        var query = _context.Clips
            .AsNoTracking()
            .Where(c => c.EventId == eventId && c.Id != excludeClipId);

        // 2. Fetch all candidates (client-side weighting due to JSON tags)
        var eventClips = await query.ToListAsync();

        // 3. Weighting Logic
        var scoredClips = eventClips.Select(c =>
        {
            int score = 0;
            if (!string.IsNullOrEmpty(c.TagsJson))
            {
                foreach (var tag in tags)
                {
                    if (c.TagsJson.Contains(tag, StringComparison.OrdinalIgnoreCase)) score += 2;
                }
            }
            return new { Clip = c, Score = score };
        });

        return scoredClips
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Clip.RecordingStartedAt)
            .Take(count)
            .Select(x => x.Clip)
            .ToList();
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
