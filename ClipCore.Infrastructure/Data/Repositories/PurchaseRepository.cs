using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PurchaseRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(Purchase purchase)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Purchases.AddAsync(purchase);
        await context.SaveChangesAsync();
    }

    public async Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        using var context = await _contextFactory.CreateDbContextAsync();
        // Explicitly exclude GIF purchases from standard license checks
        // "HasPurchasedAsync" implies "Has Purchased the Full Video License"
        return await context.Purchases
            .AnyAsync(p => p.UserId == userId && p.ClipId == clipId && p.LicenseType == license);
    }

    public async Task<bool> HasPurchasedGifAsync(string? userId, string clipId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AnyAsync(p => p.UserId == userId && p.ClipId == clipId && p.IsGif);
    }

    public async Task<Purchase?> GetGifPurchaseAsync(string? userId, string clipId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ClipId == clipId && p.IsGif);
    }

    public async Task<List<Purchase>> GetByUserIdAsync(string? userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> GetByEmailAsync(string email)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.CustomerEmail != null && p.CustomerEmail.ToLower() == email.ToLower())
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> GetByOrderNumberAsync(string email, string partialOrderId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => 
                (p.CustomerEmail != null && p.CustomerEmail.ToLower() == email.ToLower()) &&
                p.StripeSessionId.EndsWith(partialOrderId)
            )
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> GetBySessionIdAsync(string sessionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.StripeSessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<Purchase>> ListAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> ListFilteredAsync(FulfillmentStatus? status = null, DateTime? since = null, string? search = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.FulfillmentStatus == status.Value);
        }

        if (since.HasValue && string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.CreatedAt >= since.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower().Trim();
            
            // Normalize variants of the search term to handle common human errors (O vs 0)
            var sWithZero = s.Replace("o", "0");
            var sWithO = s.Replace("0", "o");
            
            // Strip ORD- prefix if present for ID searches
            var sId = s.StartsWith("ord-") ? s.Substring(4) : s;
            var sIdWithZero = sId.Replace("o", "0");
            var sIdWithO = sId.Replace("0", "o");

            query = query.Where(p => 
                (p.CustomerEmail != null && p.CustomerEmail.ToLower().Contains(s)) ||
                (p.CustomerName != null && p.CustomerName.ToLower().Contains(s)) ||
                (p.StripeSessionId != null && (
                    p.StripeSessionId.ToLower().Contains(s) || 
                    p.StripeSessionId.ToLower().Contains(sWithZero) ||
                    p.StripeSessionId.ToLower().Contains(sWithO) ||
                    p.StripeSessionId.ToLower().Contains(sId) ||
                    p.StripeSessionId.ToLower().Contains(sIdWithZero) ||
                    p.StripeSessionId.ToLower().Contains(sIdWithO)
                )) ||
                (p.OrderId != null && (
                    p.OrderId.ToLower().Contains(s) || 
                    p.OrderId.ToLower().Contains(sWithZero) ||
                    p.OrderId.ToLower().Contains(sWithO) ||
                    p.OrderId.ToLower().Contains(sId) ||
                    p.OrderId.ToLower().Contains(sIdWithZero) ||
                    p.OrderId.ToLower().Contains(sIdWithO)
                )) ||
                (p.ClipTitle != null && p.ClipTitle.ToLower().Contains(s)) ||
                (p.CollectionName != null && p.CollectionName.ToLower().Contains(s))
            );
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Purchase purchase)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existingPurchase = await context.Purchases
            .FirstOrDefaultAsync(p => p.Id == purchase.Id);

        if (existingPurchase != null)
        {
            // Update all mutable fields
            existingPurchase.FulfillmentStatus = purchase.FulfillmentStatus;
            existingPurchase.FulfilledAt = purchase.FulfilledAt;
            existingPurchase.FulfillmentMuxAssetId = purchase.FulfillmentMuxAssetId;
            existingPurchase.HighResDownloadUrl = purchase.HighResDownloadUrl;
            
            // Critical: Update fields modified during Auto-Copy or Revert
            existingPurchase.ClipMasterFileName = purchase.ClipMasterFileName;
            existingPurchase.ClipThumbnailFileName = purchase.ClipThumbnailFileName;
            existingPurchase.OrderId = purchase.OrderId;
            existingPurchase.StripeSessionId = purchase.StripeSessionId;
            
            // Critical: Allow Self-Healing of UserId
            existingPurchase.UserId = purchase.UserId;
            existingPurchase.BrandedPlaybackId = purchase.BrandedPlaybackId;

            // Update Snapshots if they were missing or corrected
            existingPurchase.ClipTitle = purchase.ClipTitle;
            existingPurchase.CollectionName = purchase.CollectionName;
            existingPurchase.CollectionDate = purchase.CollectionDate;
            existingPurchase.ClipRecordingStartedAt = purchase.ClipRecordingStartedAt;
            existingPurchase.ClipDurationSec = purchase.ClipDurationSec;
            
            // GIF Fields (Immutable mostly, but good to sync)
            existingPurchase.IsGif = purchase.IsGif;
            existingPurchase.GifStartTime = purchase.GifStartTime;
            existingPurchase.GifEndTime = purchase.GifEndTime;

            await context.SaveChangesAsync();
        }
    }

    public async Task<long> GetTotalRevenueAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases.SumAsync(p => (long)p.PricePaidCents);
    }

    public async Task<int> GetTotalSalesCountAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases.CountAsync();
    }

    public async Task<List<Purchase>> GetRecentSalesAsync(int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Dictionary<DateOnly, long>> GetDailyRevenueAsync(int days)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        
        var dailySales = await context.Purchases
            .Where(p => p.CreatedAt >= cutoff)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(p => (long)p.PricePaidCents) })
            .ToListAsync();

        return dailySales.ToDictionary(k => DateOnly.FromDateTime(k.Date), v => v.Total);
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var purchase = await context.Purchases.FindAsync(id);
        if (purchase != null)
        {
            context.Purchases.Remove(purchase);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Use ExecuteSqlRaw to truncated/delete efficiently
        // Note: TRUNCATE is faster but might need cascading. DELETE is safer for FKs if configured.
        // Given we want a clean slate, and Purchases are the leaf nodes (usually), DELETE is fine.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Purchases\"");
    }

    public async Task MigrateGifLicenseTypesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Fix for Unique Constraint Violation: Update existing GIFs to have LicenseType = Gif (2)
        // instead of reusing Personal (0). This prevents collision when a user buys both styles.
        await context.Database.ExecuteSqlRawAsync("UPDATE \"Purchases\" SET \"LicenseType\" = 2 WHERE \"IsGif\" = true AND \"LicenseType\" != 2");
    }
}
