using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Collection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class CollectionController : ControllerBase
{
    private readonly ICollectionData _collectionData;
    public CollectionController(ICollectionData collectionData) => _collectionData = collectionData;

    [Authorize(Roles = "Seller")] [HttpGet("GetCollections")]
    public async Task<IActionResult> GetCollections() =>
        Ok(await _collectionData.GetCollectionsBySeller(SellerId()));

    [Authorize(Roles = "Seller")] [HttpGet("GetCollectionDetail")]
    public async Task<IActionResult> GetCollectionDetail(string collectionId)
    {
        var coll = await _collectionData.GetCollectionDetail(collectionId, SellerId());
        return coll is null ? NotFound() : Ok(coll);
    }

    [Authorize(Roles = "Seller")] [HttpPost("CreateCollection")]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var id = await _collectionData.CreateCollection(SellerId(), request);
        return Ok(new { collectionId = id });
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateCollection")]
    public async Task<IActionResult> UpdateCollection([FromBody] UpdateCollectionRequest request)
    {
        await _collectionData.UpdateCollection(SellerId(), request);
        return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpDelete("DeleteCollection")]
    public async Task<IActionResult> DeleteCollection(string collectionId)
    {
        await _collectionData.DeleteCollection(collectionId, SellerId());
        return Ok();
    }

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")!);
}
