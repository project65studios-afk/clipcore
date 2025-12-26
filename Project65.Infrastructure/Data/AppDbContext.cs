using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project65.Core.Entities;

namespace Project65.Infrastructure.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Clip> Clips { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<DailyWatchUsage> DailyWatchUsages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event configuration
        modelBuilder.Entity<Event>()
            .HasKey(e => e.Id);
        
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Date);

        // Clip configuration
        modelBuilder.Entity<Clip>()
            .HasKey(c => c.Id);
        
        modelBuilder.Entity<Clip>()
            .Property(c => c.PriceCents)
            .IsRequired();

        // Purchase configuration
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => new { p.UserId, p.ClipId })
            .IsUnique(); // Prevent double purchase
            
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.StripeSessionId); // Index for lookup, but NOT unique (multi-item orders share ID)
    }
}
