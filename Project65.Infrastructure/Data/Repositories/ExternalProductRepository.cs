using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data;

namespace Project65.Infrastructure.Repositories;

public class ExternalProductRepository : IExternalProductRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ExternalProductRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ExternalProduct>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ExternalProducts.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<ExternalProduct?> GetByIdAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ExternalProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(ExternalProduct product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.ExternalProducts.Add(product);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExternalProduct product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.ExternalProducts.Update(product);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.ExternalProducts.FindAsync(id);
        if (product != null)
        {
            context.ExternalProducts.Remove(product);
            await context.SaveChangesAsync();
        }
    }
}
