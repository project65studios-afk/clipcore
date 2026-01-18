using Project65.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

// Configure SQLite Connection
// Use absolute path to avoid confusion
var dbPath = "/Users/carlosr/.gemini/antigravity/scratch/Project65/Project65.Web/project65.db";
var connectionString = $"Data Source={dbPath}";

var services = new ServiceCollection();
services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var serviceProvider = services.BuildServiceProvider();

var factory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
using var context = await factory.CreateDbContextAsync();

Console.WriteLine($"Database Path: {dbPath}");

try 
{
    var events = await context.Events.OrderByDescending(e => e.Date).Take(10).ToListAsync();

    Console.WriteLine("--- LAST 10 EVENTS ---");
    foreach (var evt in events)
    {
        Console.WriteLine($"ID: {evt.Id} | Name: {evt.Name} | GIF: {evt.DefaultAllowGifSale} | Price: {evt.DefaultPriceCents} | Comm: {evt.DefaultPriceCommercialCents}");
    }
    Console.WriteLine("----------------------");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
}
