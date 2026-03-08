using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(string id);
    Task<List<Collection>> ListAsync();
    /// <summary>Returns only admin-uploaded + trusted-seller collections for public marketplace.</summary>
    Task<List<Collection>> ListMarketplaceAsync();
    Task<List<Collection>> ListBySellerAsync(int sellerId);
    Task<List<Collection>> SearchAsync(string query);
    /// <summary>Searches only admin-uploaded + trusted-seller collections for public marketplace.</summary>
    Task<List<Collection>> SearchMarketplaceAsync(string query);
    Task AddAsync(Collection coll);
    Task UpdateAsync(Collection coll); // For adding clips, modifying summary
    Task DeleteAsync(string id);
}
