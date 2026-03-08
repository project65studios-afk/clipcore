using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IVideoService _videoService;
    private readonly IStorageService _storageService;

    public CollectionRepository(IDbContextFactory<AppDbContext> contextFactory, IVideoService videoService, IStorageService storageService)
    {
        _contextFactory = contextFactory;
        _videoService = videoService;
        _storageService = storageService;
    }

    public async Task<Collection?> GetByIdAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Collections
            .AsNoTracking()
            .Include(e => e.Clips)
            .Include(e => e.Seller).ThenInclude(s => s!.Storefront)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Collection>> ListAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Collections
            .AsNoTracking()
            .Include(e => e.Clips)
            .Include(e => e.Seller).ThenInclude(s => s!.Storefront)
            .AsSplitQuery()
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<List<Collection>> ListBySellerAsync(int sellerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Collections
            .AsNoTracking()
            .Include(c => c.Clips)
            .Where(c => c.SellerId == sellerId)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<List<Collection>> SearchAsync(string query)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Collections
            .AsNoTracking()
            .Include(e => e.Clips) // Include clips so we can show counts
            .Where(e => e.Name.ToLower().Contains(query.ToLower()) || (e.Summary != null && e.Summary.ToLower().Contains(query.ToLower())))
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task AddAsync(Collection coll)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Collections.AddAsync(coll);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Collection coll)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Eager load everything to ensure we're updating the graph correctly
        var existing = await context.Collections
            .FirstOrDefaultAsync(e => e.Id == coll.Id);

        if (existing != null)
        {
            existing.Name = coll.Name;
            existing.Date = coll.Date;
            existing.Location = coll.Location;
            existing.Summary = coll.Summary;
            existing.HeroClipId = coll.HeroClipId;
            existing.DefaultPriceCents = coll.DefaultPriceCents;
            existing.DefaultPriceCommercialCents = coll.DefaultPriceCommercialCents;
            existing.DefaultAllowGifSale = coll.DefaultAllowGifSale;
            existing.DefaultGifPriceCents = coll.DefaultGifPriceCents;

            // Update Clip Prices
            foreach (var clip in coll.Clips)
            {
                var existingClip = await context.Clips.FindAsync(clip.Id);
                if (existingClip != null)
                {
                    existingClip.PriceCents = clip.PriceCents;
                    existingClip.PriceCommercialCents = clip.PriceCommercialCents;
                    // Other fields are generally not editable here yet per user request
                }
            }

            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var coll = await context.Collections
            .Include(e => e.Clips)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (coll != null)
        {
            // CLEANUP: Delete External Assets (Mux & R2)
            if (coll.Clips.Any())
            {
                var cleanupTasks = new List<Task>();

                // Local helper to swallow errors individually so one failure doesn't stop others
                async Task SafeDelete(Func<Task> action)
                {
                    try { await action(); } catch { /* Ignore cleanup errors */ }
                }

                foreach (var clip in coll.Clips.ToList())
                {
                    // 1. Delete Mux Asset
                    if (!string.IsNullOrEmpty(clip.MuxAssetId))
                    {
                        cleanupTasks.Add(SafeDelete(() => _videoService.DeleteAssetAsync(clip.MuxAssetId)));
                    }

                    // 2. Delete R2 Thumbnail
                    if (!string.IsNullOrEmpty(clip.ThumbnailFileName))
                    {
                        cleanupTasks.Add(SafeDelete(() => _storageService.DeleteAsync(clip.ThumbnailFileName)));
                    }

                    // 3. Delete R2 Master (if any)
                    if (!string.IsNullOrEmpty(clip.MasterFileName))
                    {
                        cleanupTasks.Add(SafeDelete(() => _storageService.DeleteAsync(clip.MasterFileName)));
                    }
                }

                // Run all cleanup tasks in parallel
                await Task.WhenAll(cleanupTasks);

                context.Clips.RemoveRange(coll.Clips);
            }

            context.Collections.Remove(coll);
            await context.SaveChangesAsync();
        }
    }
}
