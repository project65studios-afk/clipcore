namespace Project65.Web.Models;

public class CartItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string PlaybackId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
}
