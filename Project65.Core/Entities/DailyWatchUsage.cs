using System;

namespace Project65.Core.Entities;

public class DailyWatchUsage
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TokenRequestCount { get; set; }
}
