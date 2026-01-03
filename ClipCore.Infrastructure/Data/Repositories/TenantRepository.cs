using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using ClipCore.Infrastructure.Data;

namespace ClipCore.Infrastructure.Data.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
    }
}
