using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase);
    Task<bool> HasPurchasedAsync(Guid userId, string clipId);
    Task<List<Purchase>> GetByUserIdAsync(Guid userId);
    Task<List<Purchase>> ListAsync();
    Task UpdateAsync(Purchase purchase);
}
