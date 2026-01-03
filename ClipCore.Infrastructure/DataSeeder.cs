using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Infrastructure.Data;

namespace ClipCore.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, 
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager,
        ClipCore.Web.Services.TenantContext tenantContext)
    {
        // Bootstrap TenantContext for Seeding
        // We set it to the default Seed ID so UserManager can "see" the users we are creating/checking
        tenantContext.CurrentTenant = new Tenant { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Seeding", Subdomain = "seed", OwnerId = "system" };

        // 1. Seed Default Tenant
        var defaultTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Subdomain == "demo");
        if (defaultTenant == null)
        {
            defaultTenant = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Matches FakeTenantProvider
                Name = "Demo Store",
                Subdomain = "demo",
                OwnerId = "system", // Placeholder
                CreatedAt = DateTime.UtcNow
            };
            await context.Tenants.AddAsync(defaultTenant);
            await context.SaveChangesAsync();
        }

        // 2. Seed Roles
        var adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(adminRole));
        }

        // 3. Seed Users (Platform Admin vs Store Owner logic to come later, for now just users)
        var adminEmail = "admin@clipcore.com"; // Updated domain
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TenantId = defaultTenant.Id 
            };
            await userManager.CreateAsync(user, "Admin123!");
        }

        // Assign Role
        if (!await userManager.IsInRoleAsync(user, adminRole))
        {
            await userManager.AddToRoleAsync(user, adminRole);
        }

        // Seed Regular User
        var userEmail = "carandreyn@gmail.com";
        var regularUser = await userManager.FindByEmailAsync(userEmail);
        if (regularUser == null)
        {
            regularUser = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                TenantId = defaultTenant.Id
            };
            await userManager.CreateAsync(regularUser, "User123!");
        }

        // Seed Test User
        var testEmail = "test@clipcore.com";
        var testUser = await userManager.FindByEmailAsync(testEmail);
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true,
                TenantId = defaultTenant.Id
            };
            await userManager.CreateAsync(testUser, "Test123!");
        }

        // 4. Seed Events & Clips tied to Tenant
        var existingEvents = await context.Events.IgnoreQueryFilters().ToListAsync();
        
        var events = new List<Event>
        {
            new Event
            {
                TenantId = defaultTenant.Id,
                Name = "LA Night Run",
                Location = "Los Angeles, CA",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                Summary = "Downtown neon sweeps, underpass light trails, and late-night highway ambience.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { TenantId = defaultTenant.Id, Title = "Urban Neon Loop 1", PriceCents = 999, DurationSec = 15, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_1", PlaybackIdTeaser = "fake_teaser_1" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Midnight Highway 4", PriceCents = 1999, DurationSec = 45, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_2", PlaybackIdTeaser = "fake_teaser_2" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Studio Light Sweep 7", PriceCents = 1499, DurationSec = 12, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_3", PlaybackIdTeaser = "fake_teaser_3" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Rainy Street Corner", PriceCents = 999, DurationSec = 20, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_4" }, 
                    new Clip { TenantId = defaultTenant.Id, Title = "Subway Entrance Glitch", PriceCents = 2499, DurationSec = 30, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_5", PlaybackIdTeaser = "fake_teaser_5" },
                }
            },
            new Event
            {
                TenantId = defaultTenant.Id,
                Name = "Pacific Blue",
                Location = "Malibu, CA",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)),
                Summary = "Ocean waves, coastline drones, and deep blue water textures.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { TenantId = defaultTenant.Id, Title = "Drone Coastline 1", PriceCents = 2999, DurationSec = 60, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_6", PlaybackIdTeaser = "fake_teaser_6" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Wave Crash Slowmo", PriceCents = 1599, DurationSec = 10, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_7", PlaybackIdTeaser = "fake_teaser_7" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Underwater Bubble Stream", PriceCents = 1299, DurationSec = 25, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_8" },
                }
            },
            new Event
            {
                TenantId = defaultTenant.Id,
                Name = "Warehouse Light Lab",
                Location = "Brooklyn, NY",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                Summary = "Experimental lighting setups in an abandoned industrial warehouse.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { TenantId = defaultTenant.Id, Title = "Strobe Effect Test", PriceCents = 999, DurationSec = 8, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_9", PlaybackIdTeaser = "fake_teaser_9" },
                    new Clip { TenantId = defaultTenant.Id, Title = "Laser Grid Scan", PriceCents = 1999, DurationSec = 15, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_10", PlaybackIdTeaser = "fake_teaser_10" },
                }
            }
        };

        foreach (var evt in events)
        {
            var existing = existingEvents.FirstOrDefault(e => e.Name == evt.Name && e.TenantId == defaultTenant.Id);
            if (existing != null)
            {
                // Update existing event location if missing
                if (string.IsNullOrEmpty(existing.Location))
                {
                    existing.Location = evt.Location;
                }
            }
            else
            {
                await context.Events.AddAsync(evt);
            }
        }

        // Seed Promo Codes
        if (!await context.PromoCodes.IgnoreQueryFilters().AnyAsync(p => p.Code == "TEST25" && p.TenantId == defaultTenant.Id))
        {
            await context.PromoCodes.AddAsync(new PromoCode
            {
                TenantId = defaultTenant.Id,
                Code = "TEST25",
                DiscountType = DiscountType.Percentage,
                Value = 25,
                ExpiryDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }
}
