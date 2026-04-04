using ClipCore.API.Interfaces;
using ClipCore.API.Models.Marketplace;

namespace ClipCore.API.Data;

public class MarketplaceData : IMarketplaceData
{
    private readonly ISqlDataAccess _db;
    public MarketplaceData(ISqlDataAccess db) => _db = db;

    public async Task<StorefrontPublic?> GetStorefront(string slug)
    {
        var sf = await _db.LoadSingle<StorefrontPublic, dynamic>(
            "SELECT * FROM cc_s_storefront(@Slug)", new { Slug = slug });
        if (sf is null) return null;
        sf.Clips = (await _db.LoadData<MarketplaceClip, dynamic>(
            "SELECT * FROM cc_s_storefront_clips(@Slug)", new { Slug = slug })).ToList();
        return sf;
    }

    public async Task<MarketplaceSearchResponse> SearchClips(MarketplaceSearchRequest req)
    {
        int offset = (req.Page - 1) * req.PageSize;
        var p      = new { req.SearchTerm, req.PageSize, Offset = offset };
        var total  = await _db.ExecuteScalar<int, dynamic>("SELECT cc_s_marketplace_clips_count(@SearchTerm)", p) ?? 0;
        var clips  = (await _db.LoadData<MarketplaceClip, dynamic>("SELECT * FROM cc_s_marketplace_clips(@SearchTerm,@PageSize,@Offset)", p)).ToList();
        return new MarketplaceSearchResponse { TotalCount = total, Page = req.Page, PageSize = req.PageSize, Clips = clips };
    }

    public Task<IEnumerable<MarketplaceClip>> GetFeaturedClips(int limit = 24) =>
        _db.LoadData<MarketplaceClip, dynamic>("SELECT * FROM cc_s_marketplace_clips(NULL,@Limit,0)", new { Limit = limit });
}
