using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase);
    Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license);
    Task<List<Purchase>> GetByUserIdAsync(string? userId);
    Task<List<Purchase>> GetByEmailAsync(string email);
    Task<List<Purchase>> GetByOrderNumberAsync(string email, string partialOrderId);
    Task<List<Purchase>> GetBySessionIdAsync(string sessionId);
    Task<List<Purchase>> ListAsync();
    Task UpdateAsync(Purchase purchase);
    Task<long> GetTotalRevenueAsync();
    Task<int> GetTotalSalesCountAsync();
    Task<List<Purchase>> GetRecentSalesAsync(int count);
    Task<Dictionary<DateOnly, long>> GetDailyRevenueAsync(int days);
}
