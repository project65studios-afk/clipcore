using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data.Repositories;

public class StorefrontRepository : IStorefrontRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public StorefrontRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Storefront?> GetBySlugAsync(string slug)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Storefronts
            .AsNoTracking()
            .Include(sf => sf.Seller)
            .FirstOrDefaultAsync(sf => sf.Slug == slug);
    }

    public async Task<Storefront?> GetBySellerIdAsync(int sellerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Storefronts
            .AsNoTracking()
            .FirstOrDefaultAsync(sf => sf.SellerId == sellerId);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Storefronts.AnyAsync(sf => sf.Slug == slug);
    }

    public async Task AddAsync(Storefront storefront)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Storefronts.Add(storefront);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Storefront storefront)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Storefronts.Update(storefront);
        await context.SaveChangesAsync();
    }
}
