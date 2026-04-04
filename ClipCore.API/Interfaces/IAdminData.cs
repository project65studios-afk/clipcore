using ClipCore.API.Models.Admin;

namespace ClipCore.API.Interfaces;

public interface IAdminData
{
    Task<IEnumerable<AdminSellerItem>> GetAllSellers();
    Task ApproveSeller(int sellerId);
    Task RevokeSeller(int sellerId);
    Task<PlatformStats> GetPlatformStats();
}
