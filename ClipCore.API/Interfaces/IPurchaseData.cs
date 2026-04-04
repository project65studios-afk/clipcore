using ClipCore.API.Models.Purchase;
using ClipCore.Core.Entities;

namespace ClipCore.API.Interfaces;

public interface IPurchaseData
{
    Task<IEnumerable<PurchaseItem>> GetPurchasesByUser(string userId);
    Task<IEnumerable<PurchaseItem>> GetPurchasesByEmail(string email);
    Task<IEnumerable<PurchaseItem>> GetPurchasesBySeller(int sellerId);
    Task<IEnumerable<PurchaseDetail>> GetBySessionId(string sessionId);
    Task<PurchaseDetail?> GetPurchaseDetail(int purchaseId);
    Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license);
    Task<bool> HasPurchasedGifAsync(string? userId, string clipId);
    Task<IEnumerable<PurchaseDetail>> ListFiltered(FulfillmentStatus? status, DateTime? since, string? search);
    Task<int> CreatePurchase(string? userId, string clipId, int sellerId, int pricePaidCents, int platformFeeCents, int sellerPayoutCents, string stripeSessionId, string orderId, LicenseType licenseType, string? customerEmail, string? customerName, bool isGif, double? gifStartTime, double? gifEndTime);
    Task FulfillPurchase(int purchaseId, string highResDownloadUrl, string? muxAssetId);
    Task<IEnumerable<SellerSalesSummary>> GetSellerSalesSummary();
    Task<IEnumerable<DailyRevenue>> GetDailyRevenue(int days);
    Task<IEnumerable<PurchaseItem>> GetRecentSales(int count);
    Task<bool> HasUserPurchasedClip(string? userId, string customerEmail, string clipId);
}
