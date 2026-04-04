using ClipCore.API.Models.Marketplace;

namespace ClipCore.API.Interfaces;

public interface IMarketplaceData
{
    Task<StorefrontPublic?> GetStorefront(string slug);
    Task<MarketplaceSearchResponse> SearchClips(MarketplaceSearchRequest request);
    Task<IEnumerable<MarketplaceClip>> GetFeaturedClips(int limit = 24);
}
