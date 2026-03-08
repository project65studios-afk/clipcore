using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ISellerRepository
{
    Task<Seller?> GetByUserIdAsync(string userId);
    Task<Seller?> GetByIdAsync(int id);
    Task AddAsync(Seller seller);
    Task UpdateAsync(Seller seller);
}
