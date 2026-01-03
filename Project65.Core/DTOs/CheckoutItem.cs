using Project65.Core.Entities;

namespace Project65.Core.DTOs;

public class CheckoutItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? EventName { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? ClipRecordingStartedAt { get; set; }
    public double? DurationSec { get; set; }
    public string? MasterFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public int PriceCents { get; set; }
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
}
