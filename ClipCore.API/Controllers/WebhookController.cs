using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClipCore.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace ClipCore.API.Controllers;

[ApiController]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IClipData _clipData;
    private readonly IOrderFulfillmentService _fulfillment;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IConfiguration config,
        IClipData clipData,
        IOrderFulfillmentService fulfillment,
        ILogger<WebhookController> logger)
    {
        _config      = config;
        _clipData    = clipData;
        _fulfillment = fulfillment;
        _logger      = logger;
    }

    [HttpPost("api/webhooks/mux")]
    public async Task<IActionResult> HandleMux()
    {
        using var reader = new StreamReader(Request.Body);
        var body      = await reader.ReadToEndAsync();
        var signature = Request.Headers["mux-signature"].ToString();
        var secret    = _config["Mux:WebhookSecret"] ?? "";

        if (!string.IsNullOrEmpty(secret) && !VerifyMuxSignature(body, signature, secret))
        {
            _logger.LogWarning("[MuxWebhook] Signature verification failed");
            return Unauthorized();
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root      = doc.RootElement;
            var eventType = root.GetProperty("type").GetString();
            _logger.LogInformation("[MuxWebhook] Received: {EventType}", eventType);

            switch (eventType)
            {
                case "video.asset.ready":
                    await HandleAssetReadyAsync(root);
                    break;

                case "video.asset.errored":
                    await HandleAssetErroredAsync(root);
                    break;

                default:
                    _logger.LogInformation("[MuxWebhook] Unhandled event: {EventType}", eventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MuxWebhook] Error processing event");
            return StatusCode(500);
        }
    }

    [HttpPost("api/webhooks/stripe")]
    public async Task<IActionResult> HandleStripe()
    {
        using var reader = new StreamReader(Request.Body);
        var json   = await reader.ReadToEndAsync();
        var secret = _config["Stripe:WebhookSecret"] ?? "";

        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("[StripeWebhook] No Stripe:WebhookSecret configured");
            return BadRequest("Configuration error");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], secret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("[StripeWebhook] Signature verification failed: {Message}", ex.Message);
            return BadRequest();
        }

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session is not null)
            {
                _logger.LogInformation("[StripeWebhook] Processing session {SessionId}", session.Id);
                await _fulfillment.FulfillOrderAsync(session.Id);
            }
        }
        else
        {
            _logger.LogInformation("[StripeWebhook] Unhandled event: {EventType}", stripeEvent.Type);
        }

        return Ok();
    }

    private async Task HandleAssetReadyAsync(JsonElement root)
    {
        var data      = root.GetProperty("data");
        var assetId   = data.GetProperty("id").GetString();
        var passthrough = data.TryGetProperty("passthrough", out var pt) ? pt.GetString() : null;

        if (string.IsNullOrEmpty(assetId) || string.IsNullOrEmpty(passthrough)) return;

        _logger.LogInformation("[MuxWebhook] video.asset.ready: assetId={AssetId}, clipId={ClipId}", assetId, passthrough);

        double? duration = data.TryGetProperty("duration", out var d) && d.TryGetDouble(out var dv) ? dv : null;

        // Auto-delete clips over 90 seconds
        if (duration > 90)
        {
            _logger.LogWarning("[MuxWebhook] Clip {ClipId} duration {Duration}s > 90s limit — marking errored", passthrough, duration);
            await _clipData.SetMuxData(passthrough, $"errored:{assetId}", "", null, duration, null, null);
            return;
        }

        // Parse playback IDs
        string? signedPlaybackId  = null;
        string? teaserPlaybackId  = null;
        if (data.TryGetProperty("playback_ids", out var pbIds))
        {
            foreach (var pb in pbIds.EnumerateArray())
            {
                var policy = pb.TryGetProperty("policy", out var pol) ? pol.GetString() : null;
                var pbId   = pb.TryGetProperty("id", out var pid) ? pid.GetString() : null;
                if (string.Equals(policy, "signed", StringComparison.OrdinalIgnoreCase))
                    signedPlaybackId = pbId;
                else
                    teaserPlaybackId = pbId;
            }
        }

        // Parse dimensions
        int? width = null, height = null;
        if (data.TryGetProperty("tracks", out var tracks))
        {
            foreach (var track in tracks.EnumerateArray())
            {
                if (track.TryGetProperty("type", out var tt) && tt.GetString() == "video")
                {
                    if (track.TryGetProperty("max_width",  out var w)) width  = w.GetInt32();
                    if (track.TryGetProperty("max_height", out var h)) height = h.GetInt32();
                    break;
                }
            }
        }

        await _clipData.SetMuxData(passthrough, assetId, signedPlaybackId ?? "", teaserPlaybackId, duration, width, height);
        _logger.LogInformation("[MuxWebhook] Clip {ClipId} ready — playbackId={PlaybackId}", passthrough, signedPlaybackId);
    }

    private async Task HandleAssetErroredAsync(JsonElement root)
    {
        var data      = root.GetProperty("data");
        var assetId   = data.GetProperty("id").GetString()!;
        var passthrough = data.TryGetProperty("passthrough", out var pt) ? pt.GetString() : null;

        _logger.LogWarning("[MuxWebhook] video.asset.errored: assetId={AssetId}, clipId={ClipId}", assetId, passthrough);

        if (!string.IsNullOrEmpty(passthrough))
            await _clipData.SetMuxData(passthrough, $"errored:{assetId}", "", null, null, null, null);
    }

    private static bool VerifyMuxSignature(string body, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        var parts  = signature.Split(',');
        var tsPart = parts.FirstOrDefault(p => p.StartsWith("t="));
        var v1Part = parts.FirstOrDefault(p => p.StartsWith("v1="));
        if (tsPart is null || v1Part is null) return false;
        var ts       = tsPart[2..];
        var expected = v1Part[3..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ts}.{body}"))).ToLower();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(expected));
    }
}
