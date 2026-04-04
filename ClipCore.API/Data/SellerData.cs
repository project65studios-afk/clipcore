using ClipCore.API.Interfaces;
using ClipCore.API.Models.Seller;

namespace ClipCore.API.Data;

public class SellerData : ISellerData
{
    private readonly ISqlDataAccess _db;
    public SellerData(ISqlDataAccess db) => _db = db;

    public Task<SellerProfile?> GetSellerProfile(int sellerId) =>
        _db.LoadSingle<SellerProfile, dynamic>("SELECT * FROM cc_s_seller_profile(@SellerId)", new { SellerId = sellerId });

    public Task<SellerProfile?> GetSellerProfileByUserId(string userId) =>
        _db.LoadSingle<SellerProfile, dynamic>("SELECT * FROM cc_s_seller_profile_by_user(@UserId)", new { UserId = userId });

    public async Task<int> CreateSeller(string userId) =>
        await _db.ExecuteScalar<int, dynamic>("SELECT cc_i_seller(@UserId)", new { UserId = userId })
            ?? throw new InvalidOperationException("Failed to create seller");

    public Task CreateStorefront(int sellerId, string slug, string displayName) =>
        _db.SaveData("CALL cc_i_storefront(@SellerId, @Slug, @DisplayName)", new { SellerId = sellerId, Slug = slug, DisplayName = displayName });

    public Task UpdateStorefrontSettings(int sellerId, StorefrontSettingsRequest r) =>
        _db.SaveData("CALL cc_u_storefront_settings(@SellerId, @DisplayName, @LogoUrl, @BannerUrl, @AccentColor, @Bio, @IsPublished)",
            new { SellerId = sellerId, r.DisplayName, r.LogoUrl, r.BannerUrl, r.AccentColor, r.Bio, r.IsPublished });

    public async Task<SellerSalesStats> GetSellerSalesStats(int sellerId) =>
        await _db.LoadSingle<SellerSalesStats, dynamic>("SELECT * FROM cc_s_seller_sales_stats(@SellerId)", new { SellerId = sellerId })
            ?? new SellerSalesStats();

    public async Task<bool> SlugExists(string slug) =>
        await _db.ExecuteScalar<bool, dynamic>("SELECT cc_s_slug_exists(@Slug)", new { Slug = slug }) ?? false;
}
