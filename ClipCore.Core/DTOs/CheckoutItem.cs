using ClipCore.Core.Entities;

namespace ClipCore.Core.DTOs;

public class CheckoutItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CollectionName { get; set; }
    public DateOnly? CollectionDate { get; set; }
    public DateTime? ClipRecordingStartedAt { get; set; }
    public double? DurationSec { get; set; }
    public string? MasterFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public int PriceCents { get; set; }
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
    
    public bool IsGif { get; set; } = false;
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
}
