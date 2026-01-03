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

        var email = user.FindFirstValue(ClaimTypes.Email);

        // Super Admin Bypass: admin@clipcore.com has platform-wide access
        if (email == "admin@clipcore.com") return true;

        // "Admin" role users in the TenantMembership table
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
