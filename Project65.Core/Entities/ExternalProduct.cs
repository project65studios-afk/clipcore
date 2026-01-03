using System;

namespace Project65.Core.Entities;

public class ExternalProduct
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? StorageKey { get; set; } // Key for deleting from R2
    
    // Storing as strings for flexibility with currency symbols or "Free"
    public string PriceDisplay { get; set; } = string.Empty; 
    public string? CompareAtPriceDisplay { get; set; } // For strikethrough

    public string ProductUrl { get; set; } = string.Empty;
    public string? BadgeText { get; set; } // e.g. "Limited Edition"
    public bool IsSoldOut { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many
    public List<Event> Events { get; set; } = new();
}
