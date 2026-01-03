using System.ComponentModel.DataAnnotations;

namespace ClipCore.Core.Entities;

public class Setting
{
    public Guid TenantId { get; set; }
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
