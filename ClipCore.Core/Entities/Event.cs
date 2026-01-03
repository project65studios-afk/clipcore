using System;
using System.Collections.Generic;

namespace ClipCore.Core.Entities;

public class Event
{
    public Guid TenantId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Slug, e.g. "2025-11-30"
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? HeroClipId { get; set; }

    public List<Clip> Clips { get; set; } = new();
    public List<ExternalProduct> FeaturedProducts { get; set; } = new();
}
