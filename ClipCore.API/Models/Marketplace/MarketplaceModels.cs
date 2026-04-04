namespace ClipCore.API.Models.Marketplace;

public class StorefrontPublic {
    public string  Slug { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsTrusted { get; set; }
    public List<MarketplaceClip> Clips { get; set; } = new();
}

public class MarketplaceClip {
    public string  Id { get; set; } = "";
    public string  Title { get; set; } = "";
    public string? PlaybackIdTeaser { get; set; }
    public string? ThumbnailFileName { get; set; }
    public int     PriceCents { get; set; }
    public int     PriceCommercialCents { get; set; }
    public bool    AllowGifSale { get; set; }
    public int     GifPriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string? CollectionName { get; set; }
    public string  StorefrontSlug { get; set; } = "";
}

public class MarketplaceSearchRequest {
    public string? SearchTerm { get; set; }
    public int     Page { get; set; } = 1;
    public int     PageSize { get; set; } = 24;
}

public class MarketplaceSearchResponse {
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<MarketplaceClip> Clips { get; set; } = new();
}
