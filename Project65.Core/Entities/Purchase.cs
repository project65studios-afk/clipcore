using System;

namespace Project65.Core.Entities;

public enum FulfillmentStatus
{
    Pending,
    Fulfilled
}

public class Purchase
{
    public int Id { get; set; }
    public string? UserId { get; set; } // Nullable for Guest Checkout
    public virtual ApplicationUser? User { get; set; }
    public string ClipId { get; set; } = string.Empty;
    public Clip Clip { get; set; } = null!;
    
    public string StripeSessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FulfillmentStatus FulfillmentStatus { get; set; } = FulfillmentStatus.Pending;
    public string? HighResDownloadUrl { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? FulfillmentMuxAssetId { get; set; }
    
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; } // JSON or formatted string
    public string? CustomerPhone { get; set; }
    
    public int PricePaidCents { get; set; }
}
