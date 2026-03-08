namespace ClipCore.Core.Entities;

public class Seller
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Storefront Storefront { get; set; } = null!;
    public bool IsTrusted { get; set; } = false; // Trusted sellers bypass moderation queue
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
