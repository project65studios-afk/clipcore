using ClipCore.API.Interfaces;
using ClipCore.API.Models.Marketplace;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceData _marketplace;
    public MarketplaceController(IMarketplaceData marketplace) => _marketplace = marketplace;

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
}
