using System.ComponentModel.DataAnnotations;

namespace ClipCore.Core.Entities;

public class TenantMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public Guid TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }

    [Required]
    public string Role { get; set; } = "Member"; // Member, Admin, Owner
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
