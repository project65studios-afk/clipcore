namespace ClipCore.API.Models.Collection;

public class CollectionItem {
    public string   Id { get; set; } = "";
    public string   Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string?  Location { get; set; }
    public string?  Summary { get; set; }
    public int      DefaultPriceCents { get; set; }
    public int      DefaultPriceCommercialCents { get; set; }
    public bool     DefaultAllowGifSale { get; set; }
    public int      DefaultGifPriceCents { get; set; }
    public string?  HeroClipId { get; set; }
    public int      ClipCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CollectionDetail : CollectionItem {
    public List<ClipCore.API.Models.Clip.ClipItem> Clips { get; set; } = new();
}

public class CreateCollectionRequest {
    public string   Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string?  Location { get; set; }
    public string?  Summary { get; set; }
    public int      DefaultPriceCents { get; set; } = 1000;
    public int      DefaultPriceCommercialCents { get; set; } = 4900;
    public bool     DefaultAllowGifSale { get; set; }
    public int      DefaultGifPriceCents { get; set; } = 199;
}

public class UpdateCollectionRequest : CreateCollectionRequest {
    public string  CollectionId { get; set; } = "";
    public string? HeroClipId   { get; set; }
}
