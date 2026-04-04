namespace ClipCore.API.Models.Admin;

public class AdminSellerItem {
    public int    Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool   IsTrusted { get; set; }
    public bool   IsPublished { get; set; }
    public int    ClipCount { get; set; }
    public int    SalesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlatformStats {
    public int  TotalSellers { get; set; }
    public int  TrustedSellers { get; set; }
    public int  TotalClips { get; set; }
    public int  TotalPurchases { get; set; }
    public long TotalRevenueCents { get; set; }
    public long TotalPlatformFees { get; set; }
    public long TotalPayouts { get; set; }
}

public class ApproveSellerRequest { public int SellerId { get; set; } }
