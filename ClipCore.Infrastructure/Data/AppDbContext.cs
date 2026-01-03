using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Clip> Clips { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<DailyWatchUsage> DailyWatchUsages { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PromoCode> PromoCodes { get; set; } = null!;
    public DbSet<ExternalProduct> ExternalProducts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters (Multi-Tenancy)
        var currentTenantId = _tenantProvider.TenantId;

        // Note: We use a lambda here so EF Core evaluates the property access effectively
        modelBuilder.Entity<Event>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<Clip>().HasQueryFilter(c => c.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<PromoCode>().HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<Setting>().HasQueryFilter(s => s.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a => a.TenantId == _tenantProvider.TenantId);
        modelBuilder.Entity<ExternalProduct>().HasQueryFilter(p => p.TenantId == _tenantProvider.TenantId);
        // Users are special; we might filter them manually or selectively, but let's filter for now to be safe
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.TenantId == _tenantProvider.TenantId);


        // Tenant Configuration
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Subdomain)
            .IsUnique();

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

        // Event configuration
        modelBuilder.Entity<Event>()
            .HasKey(e => e.Id);
            
        modelBuilder.Entity<Event>()
            .HasMany(e => e.FeaturedProducts)
            .WithMany(p => p.Events)
            .UsingEntity(j => j.ToTable("EventProducts"));
        
        modelBuilder.Entity<Event>()
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

        // SQLite-specific: Handle Guid as Text
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<string>();
                }
            }
        }
    }
}
