using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase);
    Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license);
    Task<bool> HasPurchasedGifAsync(string? userId, string clipId);
    Task<List<Purchase>> GetByUserIdAsync(string? userId);
    Task<List<Purchase>> GetByEmailAsync(string email);
    Task<List<Purchase>> GetByOrderNumberAsync(string email, string partialOrderId);
    Task<List<Purchase>> GetBySessionIdAsync(string sessionId);
    Task<List<Purchase>> ListAsync();
    Task<List<Purchase>> ListFilteredAsync(FulfillmentStatus? status = null, DateTime? since = null, string? search = null);
    Task UpdateAsync(Purchase purchase);
    Task<long> GetTotalRevenueAsync();
    Task<int> GetTotalSalesCountAsync();
    Task<List<Purchase>> GetRecentSalesAsync(int count);
    Task<Dictionary<DateOnly, long>> GetDailyRevenueAsync(int days);
    Task DeleteAsync(int id);
}
