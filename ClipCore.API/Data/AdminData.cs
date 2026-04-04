using ClipCore.API.Interfaces;
using ClipCore.API.Models.Admin;

namespace ClipCore.API.Data;

public class AdminData : IAdminData
{
    private readonly ISqlDataAccess _db;
    public AdminData(ISqlDataAccess db) => _db = db;

    public Task<IEnumerable<AdminSellerItem>> GetAllSellers() =>
        _db.LoadData<AdminSellerItem>("SELECT * FROM cc_s_admin_sellers()");

    public Task ApproveSeller(int sellerId) =>
        _db.SaveData("CALL cc_u_seller_approve(@SellerId)", new { SellerId = sellerId });

    public Task RevokeSeller(int sellerId) =>
        _db.SaveData("CALL cc_u_seller_revoke(@SellerId)", new { SellerId = sellerId });

    public async Task<PlatformStats> GetPlatformStats() =>
        await _db.LoadSingle<PlatformStats, dynamic>("SELECT * FROM cc_s_platform_stats()", new { }) ?? new PlatformStats();
}
