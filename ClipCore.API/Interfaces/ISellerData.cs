using ClipCore.API.Models.Seller;

namespace ClipCore.API.Interfaces;

public interface ISellerData
{
    Task<SellerProfile?> GetSellerProfile(int sellerId);
    Task<SellerProfile?> GetSellerProfileByUserId(string userId);
    Task<int> CreateSeller(string userId);
    Task CreateStorefront(int sellerId, string slug, string displayName);
    Task UpdateStorefrontSettings(int sellerId, StorefrontSettingsRequest request);
    Task<SellerSalesStats> GetSellerSalesStats(int sellerId);
    Task<bool> SlugExists(string slug);
}
