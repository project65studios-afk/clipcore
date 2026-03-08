using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace ClipCore.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, 
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager,
        IConfiguration configuration,
        bool isDevelopment)
    {
        // Seed Roles
        foreach (var role in new[] { "Admin", "Seller", "Buyer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
        }

        // Seed Admin User (Production Safe)
        var adminEmail = configuration["SEED_ADMIN_EMAIL"] ?? "admin@clipcore.com";
        var user = await userManager.FindByEmailAsync(adminEmail);
        
        // In Production, require SEED_ADMIN_PASSWORD env var. In Dev, fallback to default.
        var adminPassword = configuration["SEED_ADMIN_PASSWORD"];
        if (string.IsNullOrEmpty(adminPassword) && isDevelopment)
        {
            adminPassword = "Admin123!"; // Default dev password
        }

        // Only create Admin if we have a password (either from ENV or Dev fallback)
        if (user == null && !string.IsNullOrEmpty(adminPassword))
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, adminPassword);
            if (!result.Succeeded)
            {
                // In a real logger we'd log this, but for now console is fine for startup tasks
                Console.WriteLine($"Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Assign Role if user exists
        if (user != null && !await userManager.IsInRoleAsync(user, adminRole))
        {
            await userManager.AddToRoleAsync(user, adminRole);
        }

        // ---------------------------------------------------------
        // DEVELOPMENT DATA ONLY (Skipped in Production)
        // ---------------------------------------------------------
        if (isDevelopment)
        {
            // Seed Demo User
            var testEmail = "demo@project65.com";
            var testUser = await userManager.FindByEmailAsync(testEmail);
            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = testEmail,
                    Email = testEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(testUser, "Demo123!");
            }

            // Check for existing events to update their locations if needed
            var existingEvents = await context.Collections.ToListAsync();
            
            var events = new List<Collection>
            {
                new Collection
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
                        new Clip { Title = "Rainy Street Corner", PriceCents = 999, DurationSec = 20, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_4" }, 
                        new Clip { Title = "Subway Entrance Glitch", PriceCents = 2499, DurationSec = 30, PublishedAt = DateTime.UtcNow, PlaybackIdSigned = "fake_signed_5", PlaybackIdTeaser = "fake_teaser_5" },
                    }
                },
                new Collection
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
                new Collection
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
                    await context.Collections.AddAsync(evt);
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
}
