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
    public string? ClipId { get; set; }
    public virtual Clip? Clip { get; set; }
    
    // Snapshots for when Clip/Event are deleted
    public string? ClipTitle { get; set; }
    public string? EventName { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? ClipRecordingStartedAt { get; set; }
    public double? ClipDurationSec { get; set; }
    public string? ClipMasterFileName { get; set; }
    public string? ClipThumbnailFileName { get; set; }
    
    public string StripeSessionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
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
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
}
