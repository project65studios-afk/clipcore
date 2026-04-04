using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
using ClipCore.API.Models.Collection;
using ClipCore.API.Models.Marketplace;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceData _marketplace;
    private readonly ISqlDataAccess   _db;

    public MarketplaceController(IMarketplaceData marketplace, ISqlDataAccess db)
    {
        _marketplace = marketplace;
        _db          = db;
    }

    [HttpGet("GetStorefront")]
    public async Task<IActionResult> GetStorefront(string slug)
    {
        var sf = await _marketplace.GetStorefront(slug);
        return sf is null ? NotFound() : Ok(sf);
    }

    [HttpGet("GetFeaturedClips")]
    public async Task<IActionResult> GetFeaturedClips(int limit = 24) =>
        Ok(await _marketplace.GetFeaturedClips(limit));

    [HttpPost("SearchClips")]
    public async Task<IActionResult> SearchClips([FromBody] MarketplaceSearchRequest request) =>
        Ok(await _marketplace.SearchClips(request));

    // Public collection detail — used by the Next.js frontend on /collections/[id]
    [HttpGet("GetPublicCollection")]
    public async Task<IActionResult> GetPublicCollection(string collectionId)
    {
        var coll = await _db.LoadSingle<CollectionDetail, dynamic>(
            @"SELECT c.""Id"", c.""Name"", c.""Date"", c.""Location"", c.""Summary"",
                     c.""DefaultPriceCents"", c.""DefaultPriceCommercialCents"",
                     c.""DefaultAllowGifSale"", c.""DefaultGifPriceCents"",
                     c.""HeroClipId"", c.""CreatedAt"",
                     COUNT(cl.""Id"")::int AS ""ClipCount""
              FROM ""Collections"" c
              LEFT JOIN ""Clips"" cl ON cl.""CollectionId"" = c.""Id"" AND cl.""IsArchived"" = false
              WHERE c.""Id"" = @CollectionId GROUP BY c.""Id""",
            new { CollectionId = collectionId });

        if (coll is null) return NotFound();

        coll.Clips = (await _db.LoadData<ClipItem, dynamic>(
            "SELECT * FROM cc_s_clips_by_collection(@CollectionId)",
            new { CollectionId = collectionId })).ToList();

        return Ok(coll);
    }
}
