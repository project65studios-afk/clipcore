using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;
using Stripe;
using Stripe.Checkout;

namespace ClipCore.API.Services;

public class OrderFulfillmentService : IOrderFulfillmentService
{
    private readonly IPurchaseData _purchaseData;
    private readonly IClipData     _clipData;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderFulfillmentService> _logger;

    public OrderFulfillmentService(
        IPurchaseData purchaseData,
        IClipData clipData,
        IEmailService emailService,
        IConfiguration config,
        ILogger<OrderFulfillmentService> logger)
    {
        _purchaseData = purchaseData;
        _clipData     = clipData;
        _emailService = emailService;
        _config       = config;
        _logger       = logger;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured");
    }

    public async Task FulfillOrderAsync(string sessionId)
    {
        // Idempotency: skip if already processed
        var existing = await _purchaseData.GetBySessionId(sessionId);
        if (existing.Any())
        {
            _logger.LogInformation("[OrderFulfillment] Session {SessionId} already processed", sessionId);
            return;
        }

        // Fetch Stripe session with expanded line items and product metadata
        var svc     = new SessionService();
        var options = new SessionGetOptions();
        options.AddExpand("line_items");
        options.AddExpand("line_items.data.price.product");
        var session = await svc.GetAsync(sessionId, options);

        if (session.PaymentStatus != "paid")
        {
            _logger.LogWarning("[OrderFulfillment] Session {SessionId} not paid (status={Status})", sessionId, session.PaymentStatus);
            return;
        }

        // Derive short order ID from session suffix
        var orderId = sessionId.Length >= 8
            ? sessionId[^8..].ToUpper()
            : Guid.NewGuid().ToString("N")[..8].ToUpper();

        var userId        = session.ClientReferenceId;
        var customerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail;
        var customerName  = session.CustomerDetails?.Name;

        var lineItems = session.LineItems?.Data ?? new List<LineItem>();
        var createdIds = new List<string>();

        for (int i = 0; i < lineItems.Count; i++)
        {
            var item    = lineItems[i];
            var product = item.Price?.Product;

            // Pull metadata — prefer product-level, fall back to session-level by ClipId or index
            var clipId      = product?.Metadata?.GetValueOrDefault("ClipId");
            var licenseStr  = product?.Metadata?.GetValueOrDefault("LicenseType");
            var isGifStr    = product?.Metadata?.GetValueOrDefault("IsGif");
            var gifStartStr = product?.Metadata?.GetValueOrDefault("GifStartTime");
            var gifEndStr   = product?.Metadata?.GetValueOrDefault("GifEndTime");

            // Session-level backup by ClipId match
            if (string.IsNullOrEmpty(clipId) || string.IsNullOrEmpty(licenseStr))
            {
                bool found = false;
                if (!string.IsNullOrEmpty(clipId))
                {
                    for (int j = 0; j < 20 && !found; j++)
                    {
                        if (session.Metadata?.GetValueOrDefault($"c{j}_id") == clipId)
                        {
                            licenseStr  ??= session.Metadata.GetValueOrDefault($"c{j}_li");
                            isGifStr    ??= session.Metadata.GetValueOrDefault($"c{j}_ig");
                            gifStartStr ??= session.Metadata.GetValueOrDefault($"c{j}_gs");
                            gifEndStr   ??= session.Metadata.GetValueOrDefault($"c{j}_ge");
                            found = true;
                        }
                    }
                }

                // Fallback by index
                if (!found && i < 20)
                {
                    clipId      ??= session.Metadata?.GetValueOrDefault($"c{i}_id");
                    licenseStr  ??= session.Metadata?.GetValueOrDefault($"c{i}_li");
                    isGifStr    ??= session.Metadata?.GetValueOrDefault($"c{i}_ig");
                    gifStartStr ??= session.Metadata?.GetValueOrDefault($"c{i}_gs");
                    gifEndStr   ??= session.Metadata?.GetValueOrDefault($"c{i}_ge");
                }
            }

            if (string.IsNullOrEmpty(clipId))
            {
                _logger.LogWarning("[OrderFulfillment] Line item {Index} has no ClipId — skipping", i);
                continue;
            }

            if (createdIds.Contains(clipId)) continue; // deduplicate

            var licenseType = LicenseType.Personal;
            if (Enum.TryParse<LicenseType>(licenseStr, out var lt)) licenseType = lt;

            var isGif = string.Equals(isGifStr, "true", StringComparison.OrdinalIgnoreCase);

            double? gifStart = null;
            double? gifEnd   = null;
            if (double.TryParse(gifStartStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var gs)) gifStart = gs;
            if (double.TryParse(gifEndStr,   System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ge)) gifEnd   = ge;

            var pricePaidCents = Convert.ToInt32(item.AmountTotal ?? 0);
            var platformFee    = (int)Math.Round(pricePaidCents * 0.10);
            var sellerPayout   = pricePaidCents - platformFee;

            // Look up seller for this clip
            var clipDetail = await _clipData.GetClipDetail(clipId);
            var sellerId   = clipDetail?.SellerId ?? 0;

            if (sellerId == 0)
            {
                _logger.LogWarning("[OrderFulfillment] Clip {ClipId} has no SellerId — skipping", clipId);
                continue;
            }

            try
            {
                await _purchaseData.CreatePurchase(
                    userId, clipId, sellerId,
                    pricePaidCents, platformFee, sellerPayout,
                    sessionId, orderId, licenseType,
                    customerEmail, customerName,
                    isGif, gifStart, gifEnd);

                await _clipData.UpdateLastSoldAt(clipId);
                createdIds.Add(clipId);
                _logger.LogInformation("[OrderFulfillment] Created purchase for Clip {ClipId} in session {SessionId}", clipId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderFulfillment] Failed to create purchase for Clip {ClipId}", clipId);
            }
        }

        // Send receipt email
        if (!string.IsNullOrEmpty(customerEmail) && createdIds.Count > 0)
        {
            try
            {
                var html = BuildReceiptHtml(orderId, createdIds.Count, customerName);
                await _emailService.SendAsync(customerEmail, $"ClipCore Order Receipt (#{orderId})", html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderFulfillment] Failed to send receipt email for order {OrderId}", orderId);
            }
        }
    }

    private static string BuildReceiptHtml(string orderId, int itemCount, string? name) =>
        $@"<h2>Thank you{(string.IsNullOrEmpty(name) ? "" : $", {name}")}!</h2>
<p>Your order <strong>#{orderId}</strong> has been received ({itemCount} clip{(itemCount != 1 ? "s" : "")}).</p>
<p>Your download link(s) will be available in your account once fulfilled.</p>
<p>— The ClipCore Team</p>";
}
