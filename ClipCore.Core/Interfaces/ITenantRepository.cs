using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);
    Task UpdateAsync(Tenant tenant);
    Task<List<TenantMembership>> GetMembershipsAsync(Guid tenantId);
    Task AddMembershipAsync(TenantMembership membership);
    Task RemoveMembershipAsync(Guid membershipId);
}
