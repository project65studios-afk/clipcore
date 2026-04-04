using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClipCore.API.Identity;

// Thin EF context — manages ONLY AspNetUsers, AspNetRoles, etc.
// All business data (Clips, Sellers, Purchases...) goes through Dapper.
public class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : base(options) { }
}
