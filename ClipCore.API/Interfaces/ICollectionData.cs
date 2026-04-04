using ClipCore.API.Models.Collection;

namespace ClipCore.API.Interfaces;

public interface ICollectionData
{
    Task<IEnumerable<CollectionItem>> GetCollectionsBySeller(int sellerId);
    Task<CollectionDetail?> GetCollectionDetail(string collectionId, int sellerId);
    Task<string> CreateCollection(int sellerId, CreateCollectionRequest request);
    Task UpdateCollection(int sellerId, UpdateCollectionRequest request);
    Task DeleteCollection(string collectionId, int sellerId);
}
