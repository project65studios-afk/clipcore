using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IVideoService _videoService;
    private readonly IStorageService _storageService;

    public EventRepository(IDbContextFactory<AppDbContext> contextFactory, IVideoService videoService, IStorageService storageService)
    {
        _contextFactory = contextFactory;
        _videoService = videoService;
        _storageService = storageService;
    }

    public async Task<Event?> GetByIdAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Events
            .AsNoTracking()
            .Include(e => e.Clips)
            .Include(e => e.FeaturedProducts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> ListAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Events
            .AsNoTracking()
            .Include(e => e.Clips)
            .Include(e => e.FeaturedProducts)
            .AsSplitQuery()
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<List<Event>> SearchAsync(string query)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Events
            .AsNoTracking()
            .Include(e => e.Clips) // Include clips so we can show counts
            .Where(e => e.Name.ToLower().Contains(query.ToLower()) || (e.Summary != null && e.Summary.ToLower().Contains(query.ToLower())))
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task AddAsync(Event evt)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Events.AddAsync(evt);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event evt)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Eager load everything to ensure we're updating the graph correctly
        var existing = await context.Events
            .Include(e => e.FeaturedProducts)
            .FirstOrDefaultAsync(e => e.Id == evt.Id);
        
        if (existing != null)
        {
            existing.Name = evt.Name;
            existing.Date = evt.Date;
            existing.Location = evt.Location;
            existing.Summary = evt.Summary;
            existing.HeroClipId = evt.HeroClipId;
            existing.DefaultPriceCents = evt.DefaultPriceCents;
            existing.DefaultPriceCommercialCents = evt.DefaultPriceCommercialCents;
            existing.DefaultAllowGifSale = evt.DefaultAllowGifSale;
            existing.DefaultGifPriceCents = evt.DefaultGifPriceCents;
            
            // Update Featured Products
            existing.FeaturedProducts.Clear();
            foreach (var prod in evt.FeaturedProducts)
            {
                var trackedProd = await context.ExternalProducts.FindAsync(prod.Id);
                if (trackedProd != null)
                {
                    existing.FeaturedProducts.Add(trackedProd);
                }
            }

            // Update Clip Prices
            foreach (var clip in evt.Clips)
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
        var evt = await context.Events
            .Include(e => e.Clips)
            .FirstOrDefaultAsync(e => e.Id == id);
            
        if (evt != null)
        {
            // CLEANUP: Delete External Assets (Mux & R2)
            if (evt.Clips.Any())
            {
                var cleanupTasks = new List<Task>();

                // Local helper to swallow errors individually so one failure doesn't stop others
                async Task SafeDelete(Func<Task> action)
                {
                    try { await action(); } catch { /* Ignore cleanup errors */ }
                }

                foreach (var clip in evt.Clips.ToList()) 
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

                context.Clips.RemoveRange(evt.Clips);
            }
            
            context.Events.Remove(evt);
            await context.SaveChangesAsync();
        }
    }
}
