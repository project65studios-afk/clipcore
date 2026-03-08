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

    public DbSet<Seller> Sellers { get; set; } = null!;
    public DbSet<Storefront> Storefronts { get; set; } = null!;
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

        // Seller -> ApplicationUser (one-to-one)
        modelBuilder.Entity<Seller>()
            .HasOne(s => s.User)
            .WithOne(u => u.Seller)
            .HasForeignKey<Seller>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seller -> Storefront (one-to-one)
        modelBuilder.Entity<Storefront>()
            .HasOne(sf => sf.Seller)
            .WithOne(s => s.Storefront)
            .HasForeignKey<Storefront>(sf => sf.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Storefront>()
            .HasIndex(sf => sf.Slug)
            .IsUnique();

        // User -> Purchases
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Purchase -> Clip
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Clip)
            .WithMany()
            .HasForeignKey(p => p.ClipId)
            .OnDelete(DeleteBehavior.SetNull);

        // Purchase -> Seller
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Seller)
            .WithMany()
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Collection -> Seller
        modelBuilder.Entity<Collection>()
            .HasOne(c => c.Seller)
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Collection>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Collection>()
            .HasIndex(c => c.Date);

        modelBuilder.Entity<Collection>()
            .HasIndex(c => c.SellerId);

        // Clip -> Seller
        modelBuilder.Entity<Clip>()
            .HasOne(c => c.Seller)
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Clip>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Clip>()
            .Property(c => c.PriceCents)
            .IsRequired();

        modelBuilder.Entity<Clip>()
            .Property(c => c.PriceCommercialCents)
            .IsRequired();

        modelBuilder.Entity<Clip>()
            .HasIndex(c => c.SellerId);

        // Purchase configuration
        modelBuilder.Entity<Purchase>()
            .HasIndex(p => new { p.UserId, p.ClipId, p.LicenseType })
            .IsUnique();

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.StripeSessionId);

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.SellerId);
    }
}
