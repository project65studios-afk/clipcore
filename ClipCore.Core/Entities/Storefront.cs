namespace ClipCore.Core.Entities;

public class Storefront
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public virtual Seller Seller { get; set; } = null!;

    public string Slug { get; set; } = string.Empty;           // Source of truth for routing: /store/{slug}
    public string DisplayName { get; set; } = string.Empty;

    public string? Subdomain { get; set; }                     // Null until Phase 2
    public bool SubdomainActive { get; set; } = false;

    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }

    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
