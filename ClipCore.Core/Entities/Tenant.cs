using System.ComponentModel.DataAnnotations;

namespace ClipCore.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Subdomain { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CustomDomain { get; set; }

    [MaxLength(100)]
    public string? StripeConnectAccountId { get; set; }

    /// <summary>
    /// JSON blob for storing theme customization (colors, logos, fonts).
    /// </summary>
    public string ThemeSettingsJson { get; set; } = "{}";

    /// <summary>
    /// The ID of the user who owns this store.
    /// </summary>
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
