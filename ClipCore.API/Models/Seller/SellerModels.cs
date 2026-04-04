namespace ClipCore.API.Models.Seller;

public class SellerProfile {
    public int    Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public bool   IsTrusted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string  Slug { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsPublished { get; set; }
}

public class StorefrontSettingsRequest {
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsPublished { get; set; }
}

public class SellerSalesStats {
    public int TotalSales { get; set; }
    public int TotalRevenueCents { get; set; }
    public int TotalPayoutCents { get; set; }
    public int PendingFulfillment { get; set; }
}
