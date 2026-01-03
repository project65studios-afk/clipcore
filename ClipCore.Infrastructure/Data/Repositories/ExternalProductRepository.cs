using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Data;

namespace ClipCore.Infrastructure.Repositories;

public class ExternalProductRepository : IExternalProductRepository
{
    private readonly AppDbContext _context;

    public ExternalProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExternalProduct>> GetAllAsync()
    {
        return await _context.ExternalProducts.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<ExternalProduct?> GetByIdAsync(string id)
    {
        return await _context.ExternalProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(ExternalProduct product)
    {
        _context.ExternalProducts.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExternalProduct product)
    {
        // Safety: If an entity with this ID is already being tracked, detach it.
        // This prevents "another instance with same key is already being tracked" errors in Blazor.
        var local = _context.ExternalProducts
            .Local
            .FirstOrDefault(entry => entry.Id.Equals(product.Id));

        if (local != null)
        {
            _context.Entry(local).State = EntityState.Detached;
        }

        _context.ExternalProducts.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var product = await _context.ExternalProducts.FindAsync(id);
        if (product != null)
        {
            _context.ExternalProducts.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
