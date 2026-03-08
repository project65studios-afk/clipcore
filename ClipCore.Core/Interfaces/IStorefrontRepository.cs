using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface IStorefrontRepository
{
    Task<Storefront?> GetBySlugAsync(string slug);
    Task<Storefront?> GetBySellerIdAsync(int sellerId);
    Task<bool> SlugExistsAsync(string slug);
    Task AddAsync(Storefront storefront);
    Task UpdateAsync(Storefront storefront);
}
