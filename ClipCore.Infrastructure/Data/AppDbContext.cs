using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;

namespace ClipCore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Collection> Collections { get; set; } = null!;
    public DbSet<Clip> Clips { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<DailyWatchUsage> DailyWatchUsages { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PromoCode> PromoCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User -> Purchases Relationship
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Purchase -> Clip Relationship
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Clip)
            .WithMany()
            .HasForeignKey(p => p.ClipId)
            .OnDelete(DeleteBehavior.SetNull);

        // Collection configuration
        modelBuilder.Entity<Collection>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<Collection>()
            .HasIndex(e => e.Date);

        // Clip configuration
        modelBuilder.Entity<Clip>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Clip>()
            .Property(c => c.PriceCents)
            .IsRequired();

        modelBuilder.Entity<Clip>()
            .Property(c => c.PriceCommercialCents)
            .IsRequired();

        // Purchase configuration
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => new { p.UserId, p.ClipId, p.LicenseType })
            .IsUnique(); // Prevent double purchase of the same license

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.StripeSessionId); // Index for lookup, but NOT unique (multi-item orders share ID)
    }
}
