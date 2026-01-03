using Microsoft.AspNetCore.Identity;
using Project65.Core.Entities;

namespace Project65.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
