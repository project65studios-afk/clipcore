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

    public async Task<List<TenantMembership>> GetMembershipsAsync(Guid tenantId)
    {
        return await _context.TenantMemberships
            .Include(m => m.User)
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task AddMembershipAsync(TenantMembership membership)
    {
        await _context.TenantMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMembershipAsync(Guid membershipId)
    {
        var membership = await _context.TenantMemberships.FindAsync(membershipId);
        if (membership != null)
        {
            _context.TenantMemberships.Remove(membership);
            await _context.SaveChangesAsync();
        }
    }
}
