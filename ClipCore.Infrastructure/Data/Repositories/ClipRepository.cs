using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories;

public class ClipRepository : IClipRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ClipRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Clip?> GetByIdAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Clips
            .Include(c => c.Collection)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Clip>> SearchAsync(string query)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Clips
            .AsNoTracking()
            .Include(c => c.Collection)
            .Where(c => c.Title.ToLower().Contains(query.ToLower()) || c.TagsJson.ToLower().Contains(query.ToLower()))
            .OrderByDescending(c => c.RecordingStartedAt)
            .ToListAsync();
    }

    public async Task<List<Clip>> GetByEventIdAsync(string eventId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Clips
            .AsNoTracking()
            .Where(c => c.CollectionId == eventId)
            .OrderBy(c => c.RecordingStartedAt)
            .ToListAsync();
    }

    public async Task<List<Clip>> GetRelatedAsync(string eventId, string[] tags, string excludeClipId, int count = 4)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // 1. Get other clips from same event
        var query = context.Clips
            .AsNoTracking()
            .Where(c => c.CollectionId == eventId && c.Id != excludeClipId);

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
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Clips.AddAsync(clip);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Clip clip)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Entry(clip).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task<List<Clip>> ListBySellerAsync(int sellerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Clips
            .AsNoTracking()
            .Include(c => c.Collection)
            .Where(c => c.SellerId == sellerId)
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var clip = await context.Clips.FindAsync(id);
        if (clip != null)
        {
            context.Clips.Remove(clip);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateBatchSettingsAsync(string eventId, int priceCents, int priceCommercialCents, bool allowGif, int gifPriceCents)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Clips
            .Where(c => c.CollectionId == eventId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.PriceCents, priceCents)
                .SetProperty(c => c.PriceCommercialCents, priceCommercialCents)
                .SetProperty(c => c.AllowGifSale, allowGif)
                .SetProperty(c => c.GifPriceCents, gifPriceCents));
    }
}
