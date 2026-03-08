using System;
using System.Collections.Generic;

namespace ClipCore.Core.Entities;

public class Collection
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Slug, e.g. "2025-11-30"
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? HeroClipId { get; set; }

    public bool DefaultAllowGifSale { get; set; } = false;
    public int DefaultGifPriceCents { get; set; } = 199; // Default $1.99

    public int DefaultPriceCents { get; set; } = 1000; // Default $10.00
    public int DefaultPriceCommercialCents { get; set; } = 4900; // Default $49.00

    public List<Clip> Clips { get; set; } = new();
}
