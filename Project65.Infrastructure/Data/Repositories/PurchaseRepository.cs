using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Data.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;

    public PurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Purchase purchase)
    {
        await _context.Purchases.AddAsync(purchase);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        return await _context.Purchases
            .AnyAsync(p => p.UserId == userId && p.ClipId == clipId && p.LicenseType == license);
    }

    public async Task<List<Purchase>> GetByUserIdAsync(string? userId)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> GetByEmailAsync(string email)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.CustomerEmail != null && p.CustomerEmail.ToLower() == email.ToLower())
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> GetByOrderNumberAsync(string email, string partialOrderId)
    {
        return await _context.Purchases
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
        return await _context.Purchases
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .Where(p => p.StripeSessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<Purchase>> ListAsync()
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .ThenInclude(c => c!.Event)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> ListFilteredAsync(FulfillmentStatus? status = null, DateTime? since = null, string? search = null)
    {
        var query = _context.Purchases
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
                (p.EventName != null && p.EventName.ToLower().Contains(s))
            );
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Purchase purchase)
    {
        var existingPurchase = await _context.Purchases
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
            
            // Update Snapshots if they were missing or corrected
            existingPurchase.ClipTitle = purchase.ClipTitle;
            existingPurchase.EventName = purchase.EventName;
            existingPurchase.EventDate = purchase.EventDate;
            existingPurchase.ClipRecordingStartedAt = purchase.ClipRecordingStartedAt;
            existingPurchase.ClipDurationSec = purchase.ClipDurationSec;

            await _context.SaveChangesAsync();
            
            // Detach to allow future fresh reads if necessary (optional but helps in Blazor)
            _context.Entry(existingPurchase).State = EntityState.Detached;
        }
    }

    public async Task<long> GetTotalRevenueAsync()
    {
        return await _context.Purchases.SumAsync(p => (long)p.PricePaidCents);
    }

    public async Task<int> GetTotalSalesCountAsync()
    {
        return await _context.Purchases.CountAsync();
    }

    public async Task<List<Purchase>> GetRecentSalesAsync(int count)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Clip)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Dictionary<DateOnly, long>> GetDailyRevenueAsync(int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        
        var dailySales = await _context.Purchases
            .Where(p => p.CreatedAt >= cutoff)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(p => (long)p.PricePaidCents) })
            .ToListAsync();

        return dailySales.ToDictionary(k => DateOnly.FromDateTime(k.Date), v => v.Total);
    }
}
