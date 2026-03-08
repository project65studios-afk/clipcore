using Microsoft.AspNetCore.Identity;
using ClipCore.Core.Entities;

namespace ClipCore.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
