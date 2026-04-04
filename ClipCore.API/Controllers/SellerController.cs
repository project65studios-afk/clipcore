using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Seller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class SellerController : ControllerBase
{
    private readonly ISellerData _sellerData;
    public SellerController(ISellerData sellerData) => _sellerData = sellerData;

    [Authorize(Roles = "Seller")] [HttpGet("GetSellerProfile")]
    public async Task<IActionResult> GetSellerProfile()
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _sellerData.GetSellerProfileByUserId(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateStorefrontSettings")]
    public async Task<IActionResult> UpdateStorefrontSettings([FromBody] StorefrontSettingsRequest request)
    {
        await _sellerData.UpdateStorefrontSettings(SellerId(), request);
        return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpGet("GetSellerSalesStats")]
    public async Task<IActionResult> GetSellerSalesStats() =>
        Ok(await _sellerData.GetSellerSalesStats(SellerId()));

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")!);
}
