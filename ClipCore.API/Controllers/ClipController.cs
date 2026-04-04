using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
using ClipCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class ClipController : ControllerBase
{
    private readonly IClipData   _clipData;
    private readonly IMuxService _mux;

    public ClipController(IClipData clipData, IMuxService mux)
    {
        _clipData = clipData;
        _mux      = mux;
    }

    [Authorize(Roles = "Seller")] [HttpGet("GetClips")]
    public async Task<IActionResult> GetClips() =>
        Ok(await _clipData.GetClipsBySeller(SellerId()));

    [Authorize(Roles = "Seller")] [HttpGet("GetClipDetail")]
    public async Task<IActionResult> GetClipDetail(string clipId)
    {
        var clip = await _clipData.GetClipDetailForSeller(clipId, SellerId());
        return clip is null ? NotFound() : Ok(clip);
    }

    [Authorize(Roles = "Seller")] [HttpGet("GetMuxUploadUrl")]
    public async Task<IActionResult> GetMuxUploadUrl(string clipId)
    {
        var (uploadUrl, uploadId) = await _mux.CreateDirectUploadAsync();
        await _clipData.SetMuxUploadId(clipId, uploadId);
        return Ok(new { uploadUrl, uploadId });
    }

    [Authorize(Roles = "Seller")] [HttpPost("CreateClip")]
    public async Task<IActionResult> CreateClip([FromBody] CreateClipRequest request)
    {
        var clipId = await _clipData.CreateClip(SellerId(), request);
        return Ok(new { clipId });
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateClip")]
    public async Task<IActionResult> UpdateClip([FromBody] UpdateClipRequest request)
    {
        await _clipData.UpdateClip(SellerId(), request);
        return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateBatchSettings")]
    public async Task<IActionResult> UpdateBatchSettings([FromBody] BatchSettingsRequest request)
    {
        await _clipData.UpdateBatchSettings(
            request.CollectionId, SellerId(),
            request.PriceCents, request.PriceCommercialCents,
            request.AllowGifSale, request.GifPriceCents);
        return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpDelete("DeleteClip")]
    public async Task<IActionResult> DeleteClip(string clipId)
    {
        var clip = await _clipData.GetClipDetailForSeller(clipId, SellerId());
        if (clip?.MuxAssetId is not null) await _mux.DeleteAssetAsync(clip.MuxAssetId);
        await _clipData.DeleteClip(clipId, SellerId());
        return Ok();
    }

    // Public — for purchase flow. Strips signed playback ID.
    [HttpGet("GetPublicClipDetail")]
    public async Task<IActionResult> GetPublicClipDetail(string clipId)
    {
        var clip = await _clipData.GetClipDetail(clipId);
        if (clip is null || clip.IsArchived) return NotFound();
        clip.PlaybackIdSigned = string.Empty; // Never expose to unauthenticated callers
        return Ok(clip);
    }

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")
        ?? throw new UnauthorizedAccessException("No seller_id claim"));
}
