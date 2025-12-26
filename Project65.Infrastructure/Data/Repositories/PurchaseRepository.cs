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

    public async Task<bool> HasPurchasedAsync(Guid userId, string clipId)
    {
        return await _context.Purchases
            .AnyAsync(p => p.UserId == userId && p.ClipId == clipId);
    }

    public async Task<List<Purchase>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Purchases
            .Include(p => p.Clip)
            .ThenInclude(c => c.Event)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Purchase>> ListAsync()
    {
        return await _context.Purchases
            .Include(p => p.Clip)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Purchase purchase)
    {
        _context.Purchases.Update(purchase);
        await _context.SaveChangesAsync();
    }
}
