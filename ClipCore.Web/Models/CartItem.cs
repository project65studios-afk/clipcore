using ClipCore.Core.Entities;

namespace ClipCore.Web.Models;

public class CartItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string PlaybackId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string? EventName { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? ClipRecordingStartedAt { get; set; }
    public string? MasterFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
}
