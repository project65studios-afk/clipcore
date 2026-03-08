using System;
using System.Collections.Generic;

namespace ClipCore.Core.Entities;

public class Clip
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CollectionId { get; set; } = string.Empty;
    public Collection Collection { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public int PriceCommercialCents { get; set; }
    
    public bool AllowGifSale { get; set; } = false;
    public int GifPriceCents { get; set; } = 199; // Default $1.99
    
    public string PlaybackIdSigned { get; set; } = string.Empty;
    public string? PlaybackIdTeaser { get; set; }

    public double? DurationSec { get; set; } // Mux usually provides double
    public DateTime? RecordingStartedAt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    
    public string TagsJson { get; set; } = "[]"; // Simple JSON array storage
    
    public string? MuxUploadId { get; set; }
    public string? MuxAssetId { get; set; }
    
    public string? MasterFileName { get; set; }
    public string? ThumbnailFileName { get; set; } // Local high-res thumbnail
    public bool IsDirectUpload { get; set; } = false;
    
    public int? SellerId { get; set; }
    public virtual Seller? Seller { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    // Archive state — Mux asset deleted to save costs, R2 master kept
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public DateTime? LastSoldAt { get; set; }
}
