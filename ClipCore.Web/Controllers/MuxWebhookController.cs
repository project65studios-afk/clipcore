using Microsoft.AspNetCore.Mvc;
using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClipCore.Web.Controllers;

[Route("api/webhooks/mux")]
[ApiController]
public class MuxWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IClipRepository _clipRepository;
    private readonly IVideoService _videoService;
    private readonly IHubContext<ClipCore.Web.Hubs.ProcessingHub> _hubContext;
    private readonly ILogger<MuxWebhookController> _logger;

    public MuxWebhookController(
        IConfiguration configuration,
        IClipRepository clipRepository,
        IVideoService videoService,
        IHubContext<ClipCore.Web.Hubs.ProcessingHub> hubContext,
        ILogger<MuxWebhookController> logger)
    {
        _configuration = configuration;
        _clipRepository = clipRepository;
        _videoService = videoService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var webhookSecret = _configuration["Mux:WebhookSecret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            if (!Request.Headers.TryGetValue("Mux-Signature", out var sigHeader))
            {
                _logger.LogWarning("[MuxWebhook] Missing Mux-Signature header");
                return Unauthorized();
            }

            if (!VerifySignature(json, sigHeader!, webhookSecret))
            {
                _logger.LogWarning("[MuxWebhook] Signature verification failed");
                return Unauthorized();
            }
        }
        else
        {
            _logger.LogWarning("[MuxWebhook] No Mux:WebhookSecret configured — skipping signature check");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var eventType = root.GetProperty("type").GetString();
            _logger.LogInformation("[MuxWebhook] Received event: {EventType}", eventType);

            switch (eventType)
            {
                case "video.asset.ready":
                    await HandleAssetReadyAsync(root);
                    break;

                case "video.asset.errored":
                    await HandleAssetErroredAsync(root);
                    break;

                default:
                    _logger.LogInformation("[MuxWebhook] Unhandled event type: {EventType}", eventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MuxWebhook] Error processing webhook");
            return StatusCode(500);
        }
    }

    private async Task HandleAssetReadyAsync(JsonElement root)
    {
        var data = root.GetProperty("data");
        var assetId = data.GetProperty("id").GetString();
        var passthrough = data.TryGetProperty("passthrough", out var pt) ? pt.GetString() : null;

        if (string.IsNullOrEmpty(assetId))
        {
            _logger.LogWarning("[MuxWebhook] video.asset.ready missing assetId");
            return;
        }

        _logger.LogInformation("[MuxWebhook] video.asset.ready: assetId={AssetId}, passthrough={Passthrough}", assetId, passthrough);

        // passthrough = clipId (set during CreateUploadUrlAsync)
        if (string.IsNullOrEmpty(passthrough)) return;

        var clip = await _clipRepository.GetByIdAsync(passthrough);
        if (clip == null)
        {
            _logger.LogWarning("[MuxWebhook] No clip found for passthrough={ClipId}", passthrough);
            return;
        }

        clip.MuxAssetId = assetId;

        // Read duration from webhook data if available
        if (data.TryGetProperty("duration", out var durEl) && durEl.TryGetDouble(out var duration))
        {
            clip.DurationSec = duration;

            // Server-side enforcement: clips > 90s get deleted
            if (duration > 90)
            {
                _logger.LogWarning("[MuxWebhook] Clip {ClipId} duration {Duration}s exceeds 90s limit. Deleting.", clip.Id, duration);
                await _videoService.DeleteAssetAsync(assetId);
                await _clipRepository.DeleteAsync(clip.Id);
                await _hubContext.Clients.All.SendAsync("ClipStatusUpdated", clip.Id, "Rejected:TooLong");
                return;
            }
        }

        // Read recording time from master_access or recording_times
        if (data.TryGetProperty("recording_times", out var recTimes) && recTimes.ValueKind == JsonValueKind.Array)
        {
            foreach (var rt in recTimes.EnumerateArray())
            {
                if (rt.TryGetProperty("started_at", out var sa))
                {
                    var startedAtStr = sa.GetString();
                    if (DateTime.TryParse(startedAtStr, out var startedAt) && !clip.RecordingStartedAt.HasValue)
                    {
                        clip.RecordingStartedAt = startedAt.ToUniversalTime();
                    }
                    break;
                }
            }
        }

        // Ensure playback ID
        var playbackId = await _videoService.EnsurePlaybackIdAsync(assetId);
        if (!string.IsNullOrEmpty(playbackId))
        {
            clip.PlaybackIdSigned = playbackId;
        }

        await _clipRepository.UpdateAsync(clip);
        await _hubContext.Clients.All.SendAsync("ClipStatusUpdated", clip.Id, "Ready");
        _logger.LogInformation("[MuxWebhook] Clip {ClipId} updated: Ready (duration={Duration}s, playbackId={PlaybackId})", clip.Id, clip.DurationSec, clip.PlaybackIdSigned);
    }

    private async Task HandleAssetErroredAsync(JsonElement root)
    {
        var data = root.GetProperty("data");
        var assetId = data.GetProperty("id").GetString();
        var passthrough = data.TryGetProperty("passthrough", out var pt) ? pt.GetString() : null;

        _logger.LogWarning("[MuxWebhook] video.asset.errored: assetId={AssetId}, passthrough={Passthrough}", assetId, passthrough);

        if (!string.IsNullOrEmpty(passthrough))
        {
            var clip = await _clipRepository.GetByIdAsync(passthrough);
            if (clip != null)
            {
                // Mark clip as errored by clearing MuxAssetId but keeping record for seller visibility
                clip.MuxAssetId = $"errored:{assetId}";
                await _clipRepository.UpdateAsync(clip);
                await _hubContext.Clients.All.SendAsync("ClipStatusUpdated", clip.Id, "Errored");
                _logger.LogWarning("[MuxWebhook] Clip {ClipId} marked as errored", clip.Id);
            }
        }
    }

    /// <summary>
    /// Verifies the Mux webhook signature (HMAC-SHA256).
    /// Mux-Signature header format: t=timestamp,v1=signature
    /// </summary>
    private static bool VerifySignature(string payload, string signatureHeader, string secret)
    {
        try
        {
            var parts = signatureHeader.Split(',');
            var timestamp = parts.FirstOrDefault(p => p.StartsWith("t="))?.Substring(2);
            var signature = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3);

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature)) return false;

            var signedPayload = $"{timestamp}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var expected = Convert.ToHexString(hash).ToLower();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature));
        }
        catch
        {
            return false;
        }
    }
}
