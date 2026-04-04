namespace ClipCore.API.Models.Clip;

public class ClipItem {
    public string  Id { get; set; } = "";
    public string  Title { get; set; } = "";
    public string  CollectionId { get; set; } = "";
    public string? CollectionName { get; set; }
    public int     PriceCents { get; set; }
    public int     PriceCommercialCents { get; set; }
    public bool    AllowGifSale { get; set; }
    public int     GifPriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string? PlaybackIdTeaser { get; set; }
    public string? ThumbnailFileName { get; set; }
    public bool    IsArchived { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class ClipDetail : ClipItem {
    public string  PlaybackIdSigned { get; set; } = "";
    public string? MuxAssetId { get; set; }
    public string? MuxUploadId { get; set; }
    public string? MasterFileName { get; set; }
    public string  TagsJson { get; set; } = "[]";
    public DateTime? RecordingStartedAt { get; set; }
    public int?    Width { get; set; }
    public int?    Height { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? LastSoldAt { get; set; }
    public int?    SellerId { get; set; }
}

public class CreateClipRequest {
    public string CollectionId { get; set; } = "";
    public string Title { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; } = 199;
    public string TagsJson { get; set; } = "[]";
}

public class UpdateClipRequest {
    public string ClipId { get; set; } = "";
    public string Title { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; }
    public string TagsJson { get; set; } = "[]";
}

public class BatchSettingsRequest {
    public string CollectionId { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; }
}

public class ArchiveCandidateClip {
    public string  Id { get; set; } = "";
    public string? MuxAssetId { get; set; }
}
