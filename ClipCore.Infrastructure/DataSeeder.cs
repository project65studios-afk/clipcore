using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Infrastructure.Data;

namespace ClipCore.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, 
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager)
    {
        // Seed Roles
        var adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(adminRole));
        }

        // Seed Admin User
        var adminEmail = "admin@project65.com";
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
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
                EmailConfirmed = true
            };
            await userManager.CreateAsync(regularUser, "User123!");
        }

        // Seed Test User (User Request)
        var testEmail = "test@project65.com";
        var testUser = await userManager.FindByEmailAsync(testEmail);
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(testUser, "Test123!");
        }

        // Check for existing events to update their locations if needed
        var existingEvents = await context.Events.ToListAsync();
        
        var events = new List<Event>
        {
            new Event
            {
                Name = "LA Night Run",
                Location = "Los Angeles, CA",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                Summary = "Downtown neon sweeps, underpass light trails, and late-night highway ambience.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { Title = "Urban Neon Loop 1", PriceCents = 999, DurationSec = 15, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_1", PlaybackIdTeaser = "fake_teaser_1" },
                    new Clip { Title = "Midnight Highway 4", PriceCents = 1999, DurationSec = 45, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_2", PlaybackIdTeaser = "fake_teaser_2" },
                    new Clip { Title = "Studio Light Sweep 7", PriceCents = 1499, DurationSec = 12, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_3", PlaybackIdTeaser = "fake_teaser_3" },
                    new Clip { Title = "Rainy Street Corner", PriceCents = 999, DurationSec = 20, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_4" }, // No teaser
                    new Clip { Title = "Subway Entrance Glitch", PriceCents = 2499, DurationSec = 30, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_5", PlaybackIdTeaser = "fake_teaser_5" },
                }
            },
            new Event
            {
                Name = "Pacific Blue",
                Location = "Malibu, CA",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)),
                Summary = "Ocean waves, coastline drones, and deep blue water textures.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { Title = "Drone Coastline 1", PriceCents = 2999, DurationSec = 60, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_6", PlaybackIdTeaser = "fake_teaser_6" },
                    new Clip { Title = "Wave Crash Slowmo", PriceCents = 1599, DurationSec = 10, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_7", PlaybackIdTeaser = "fake_teaser_7" },
                    new Clip { Title = "Underwater Bubble Stream", PriceCents = 1299, DurationSec = 25, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_8" },
                }
            },
            new Event
            {
                Name = "Warehouse Light Lab",
                Location = "Brooklyn, NY",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                Summary = "Experimental lighting setups in an abandoned industrial warehouse.",
                CreatedAt = DateTime.UtcNow,
                Clips = new List<Clip>
                {
                    new Clip { Title = "Strobe Effect Test", PriceCents = 999, DurationSec = 8, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_9", PlaybackIdTeaser = "fake_teaser_9" },
                    new Clip { Title = "Laser Grid Scan", PriceCents = 1999, DurationSec = 15, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_10", PlaybackIdTeaser = "fake_teaser_10" },
                }
            }
        };

        foreach (var evt in events)
        {
            var existing = existingEvents.FirstOrDefault(e => e.Name == evt.Name);
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
                // Add new event
                await context.Events.AddAsync(evt);
            }
        }

        // Seed Promo Codes
        if (!await context.PromoCodes.AnyAsync(p => p.Code == "TEST25"))
        {
            await context.PromoCodes.AddAsync(new PromoCode
            {
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
