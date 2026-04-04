using ClipCore.Core.Entities;

namespace ClipCore.API.Models.Purchase;

public class PurchaseItem {
    public int     Id { get; set; }
    public string? ClipId { get; set; }
    public string  ClipTitle { get; set; } = "";
    public string? CollectionName { get; set; }
    public DateOnly? CollectionDate { get; set; }
    public int     PricePaidCents { get; set; }
    public LicenseType LicenseType { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? HighResDownloadUrl { get; set; }
    public bool    IsGif { get; set; }
}

public class PurchaseDetail : PurchaseItem {
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public int     PlatformFeeCents { get; set; }
    public int     SellerPayoutCents { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? StripeSessionId { get; set; }
    public string? OrderId { get; set; }
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
    public string? BrandedPlaybackId { get; set; }
}

public class CreateCheckoutRequest {
    public string ClipId { get; set; } = "";
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
    public string? PromoCode { get; set; }
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
}

public class SellerSalesSummary {
    public int    SellerId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Slug { get; set; } = "";
    public long   SalesCount { get; set; }
    public long   TotalRevenueCents { get; set; }
    public long   PlatformFeeCents { get; set; }
    public long   SellerPayoutCents { get; set; }
}

public class DailyRevenue {
    public DateOnly Date { get; set; }
    public long     TotalCents { get; set; }
}
