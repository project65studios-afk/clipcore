using ClipCore.API.Interfaces;
using ClipCore.API.Models.Admin;
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminData    _adminData;
    private readonly IPurchaseData _purchaseData;

    public AdminController(IAdminData adminData, IPurchaseData purchaseData)
    {
        _adminData    = adminData;
        _purchaseData = purchaseData;
    }

    [HttpGet("GetSellers")]
    public async Task<IActionResult> GetSellers() => Ok(await _adminData.GetAllSellers());

    [HttpGet("GetPlatformStats")]
    public async Task<IActionResult> GetPlatformStats() => Ok(await _adminData.GetPlatformStats());

    [HttpPost("ApproveSeller")]
    public async Task<IActionResult> ApproveSeller([FromBody] ApproveSellerRequest r)
    {
        await _adminData.ApproveSeller(r.SellerId);
        return Ok();
    }

    [HttpPost("RevokeSeller")]
    public async Task<IActionResult> RevokeSeller([FromBody] ApproveSellerRequest r)
    {
        await _adminData.RevokeSeller(r.SellerId);
        return Ok();
    }

    [HttpGet("GetSellerSalesSummary")]
    public async Task<IActionResult> GetSellerSalesSummary() => Ok(await _purchaseData.GetSellerSalesSummary());

    [HttpGet("GetDailyRevenue")]
    public async Task<IActionResult> GetDailyRevenue(int days = 30) => Ok(await _purchaseData.GetDailyRevenue(days));

    [HttpGet("GetRecentSales")]
    public async Task<IActionResult> GetRecentSales(int count = 20) => Ok(await _purchaseData.GetRecentSales(count));

    [HttpGet("GetAllPurchases")]
    public async Task<IActionResult> GetAllPurchases(int? status, DateTime? since, string? search) =>
        Ok(await _purchaseData.ListFiltered(
            status.HasValue ? (FulfillmentStatus?)status.Value : null,
            since, search));
}
