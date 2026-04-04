using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace ClipCore.API;

public static class DataSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        // Seed roles
        foreach (var role in new[] { "Admin", "Seller", "Buyer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed admin user
        var adminEmail    = configuration["SEED_ADMIN_EMAIL"]    ?? "admin@clipcore.com";
        var adminPassword = configuration["SEED_ADMIN_PASSWORD"] ?? "Admin123!";

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is null)
        {
            var admin = new ApplicationUser
            {
                UserName       = adminEmail,
                Email          = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                Console.WriteLine($"[DataSeeder] Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else if (!await userManager.IsInRoleAsync(existing, "Admin"))
        {
            await userManager.AddToRoleAsync(existing, "Admin");
        }
    }
}
