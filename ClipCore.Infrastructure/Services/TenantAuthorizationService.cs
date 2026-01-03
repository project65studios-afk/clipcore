using System.Security.Claims;
using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClipCore.Infrastructure.Services;

public class TenantAuthorizationService : ITenantAuthorizationService
{
    private readonly AppDbContext _context;

    public TenantAuthorizationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAdminAsync(ClaimsPrincipal user, Guid tenantId)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;

        // "Admin" role users in the TenantMembership table, OR platform-wide super admins (if we had them)
        return await _context.TenantMemberships
            .AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && (tm.Role == "Admin" || tm.Role == "Owner"));
    }

    public async Task<bool> IsOwnerAsync(ClaimsPrincipal user, Guid tenantId)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;

        return await _context.TenantMemberships
            .AnyAsync(tm => tm.UserId == userId && tm.TenantId == tenantId && tm.Role == "Owner");
    }
}
