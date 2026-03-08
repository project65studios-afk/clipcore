using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories;

public class SellerRepository : ISellerRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SellerRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Seller?> GetByUserIdAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Sellers
            .AsNoTracking()
            .Include(s => s.Storefront)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Seller?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Sellers
            .AsNoTracking()
            .Include(s => s.Storefront)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Seller seller)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Sellers.Add(seller);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Seller seller)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Sellers.Update(seller);
        await context.SaveChangesAsync();
    }
}
