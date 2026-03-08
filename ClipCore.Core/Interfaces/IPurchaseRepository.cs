using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public class SellerSalesSummary
{
    public int SellerId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Slug { get; set; } = "";
    public int SalesCount { get; set; }
    public long TotalRevenueCents { get; set; }
    public long PlatformFeeCents { get; set; }
    public long SellerPayoutCents { get; set; }
}

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase);
    Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license);
    Task<bool> HasPurchasedGifAsync(string? userId, string clipId);
    Task<Purchase?> GetGifPurchaseAsync(string? userId, string clipId);
    Task<List<Purchase>> GetByUserIdAsync(string? userId);
    Task<List<Purchase>> GetByEmailAsync(string email);
    Task<List<Purchase>> GetByOrderNumberAsync(string email, string partialOrderId);
    Task<List<Purchase>> GetBySessionIdAsync(string sessionId);
    Task<List<Purchase>> ListAsync();
    Task<List<Purchase>> ListBySellerAsync(int sellerId);
    Task<List<Purchase>> ListFilteredAsync(FulfillmentStatus? status = null, DateTime? since = null, string? search = null);
    Task UpdateAsync(Purchase purchase);
    Task<long> GetTotalRevenueAsync();
    Task<long> GetTotalPlatformFeeAsync();
    Task<int> GetTotalSalesCountAsync();
    Task<List<Purchase>> GetRecentSalesAsync(int count);
    Task<Dictionary<DateOnly, long>> GetDailyRevenueAsync(int days);
    Task<List<SellerSalesSummary>> GetSellerSalesSummaryAsync();
    Task DeleteAsync(int id);
    Task DeleteAllAsync();
    Task MigrateGifLicenseTypesAsync();
}
