using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Components; // For NavigationManager if needed, though usually not in service
using Microsoft.AspNetCore.Components.Authorization;

using Microsoft.Extensions.Logging;

namespace Project65.Web.Services;

public class OrderFulfillmentService
{
    private readonly IPaymentService _paymentService;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IClipRepository _clipRepository;
    private readonly Project65.Web.Services.CartService _cartService;
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly EmailTemplateService _emailTemplateService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly Project65.Web.Services.StoreSettingsService _storeSettingsService;
    private readonly IVideoService _videoService;
    private readonly IStorageService _storageService;
    private readonly ILogger<OrderFulfillmentService> _logger;

    public OrderFulfillmentService(
        IPaymentService paymentService,
        IPurchaseRepository purchaseRepository,
        IClipRepository clipRepository,
        Project65.Web.Services.CartService cartService,
        IPromoCodeRepository promoCodeRepository,
        IAuditService auditService,
        IEmailService emailService,
        EmailTemplateService emailTemplateService,
        AuthenticationStateProvider authenticationStateProvider,
        Project65.Web.Services.StoreSettingsService storeSettingsService,
        IVideoService videoService,
        IStorageService storageService,
        ILogger<OrderFulfillmentService> logger)
    {
        _paymentService = paymentService;
        _purchaseRepository = purchaseRepository;
        _clipRepository = clipRepository;
        _cartService = cartService;
        _promoCodeRepository = promoCodeRepository;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _authenticationStateProvider = authenticationStateProvider;
        _storeSettingsService = storeSettingsService;
        _videoService = videoService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<List<Purchase>> FulfillOrderAsync(string sessionId)
    {
        // 0. Check for existing processing (Idempotency)
        var existingPurchases = await _purchaseRepository.GetBySessionIdAsync(sessionId);
        if (existingPurchases.Any())
        {
            // SELF-HEALING: Claim Orphaned Purchases (Ghost Orders)
            // If webhooks processed the order without UserId context, but now we have it.
            string? currentUserId = null;
            try 
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true) 
                {
                     currentUserId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }
            } catch {}

            if (currentUserId != null && existingPurchases.Any(p => string.IsNullOrEmpty(p.UserId)))
            {
                 var orphanCount = existingPurchases.Count(p => string.IsNullOrEmpty(p.UserId));
                 _logger.LogInformation($"[OrderFulfillment] Self-Healing: Claiming {orphanCount} orphaned purchases for User {currentUserId}");
                 foreach (var p in existingPurchases)
                 {
                     if (string.IsNullOrEmpty(p.UserId))
                     {
                         p.UserId = currentUserId;
                         await _purchaseRepository.UpdateAsync(p);
                     }
                 }
            }

            _logger.LogInformation($"[OrderFulfillment] Session {sessionId} already processed. Returning {existingPurchases.Count} items.");
            return existingPurchases;
        }

        // 1. Get Purchases from Stripe Session
        var purchases = (await _paymentService.GetPurchasesFromSessionAsync(sessionId)).ToList();

        if (!purchases.Any())
        {
            return new List<Purchase>();
        }

        // 2. Identify User
        // Note: In Webhook context, AuthenticationStateProvider might not have user, 
        // but purchases should have UserId from Stripe metadata if it was set during checkout creation.
        // We will try to resolve it from AuthState if redundant check needed, but mostly rely on what's in 'purchases'
        string? userId = null;
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
        }
        catch
        {
            // Ignore auth state errors (e.g. if running in background/webhook where no http context)
        }

        // Use standard 8-char suffix of Stripe Session ID for Order ID
        var shortOrderId = !string.IsNullOrEmpty(sessionId) && sessionId.Length >= 8
            ? sessionId.Substring(sessionId.Length - 8).ToUpper()
            : Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        foreach (var p in purchases)
        {
            if (p.UserId == null && userId != null) p.UserId = userId;

            // Only check duplication if we have a userId and ClipId
            bool shouldAdd = true;
            if (!string.IsNullOrEmpty(p.UserId) && !string.IsNullOrEmpty(p.ClipId))
            {
                if (await _purchaseRepository.HasPurchasedAsync(p.UserId, p.ClipId, p.LicenseType))
                {
                    shouldAdd = false;
                }
            }

            if (shouldAdd && !string.IsNullOrEmpty(p.ClipId))
            {
                if (p.IsGif)
                {
                    p.FulfillmentStatus = FulfillmentStatus.Fulfilled;
                    p.FulfilledAt = DateTime.UtcNow;
                }
                else
                {
                    p.FulfillmentStatus = FulfillmentStatus.Pending;
                }
                p.StripeSessionId = sessionId;
                p.OrderId = shortOrderId;
                p.CreatedAt = DateTime.UtcNow;

                // Healing Snapshots: If Stripe metadata failed but clip exists in DB, populate snapshots
                var dbClip = await _clipRepository.GetByIdAsync(p.ClipId!);
                if (dbClip != null)
                {
                    p.Clip = dbClip; // Link for Email Template
                    if (!p.ClipDurationSec.HasValue) p.ClipDurationSec = dbClip.DurationSec;

                    // CRITICAL: Explicitly clear any filename that might have come from Stripe Metadata or other sources.
                    // We WANT this to be null so the system knows no order-specific file exists yet.
                    p.ClipMasterFileName = null;
                    p.ClipThumbnailFileName = null;

                    // --- BRANDING LOGIC START ---
                    if (p.IsGif && string.IsNullOrEmpty(p.BrandedPlaybackId))
                    {
                        try
                        {
                            // 1. Prioritize GIF-specific watermark setting
                            var logo = await _storeSettingsService.GetGifWatermarkUrlAsync();

                            // 2. Fallback to general brand logo if GIF-specific setting is empty
                            if (string.IsNullOrEmpty(logo))
                            {
                                logo = await _storeSettingsService.GetBrandLogoUrlAsync();
                            }

                            if (!string.IsNullOrEmpty(logo) && !string.IsNullOrEmpty(dbClip.MuxAssetId))
                            {
                                // Resolve internal R2 path to public signed URL for Mux
                                if (!logo.StartsWith("http")) logo = _storageService.GetPresignedDownloadUrl(logo);

                                // Create the watermarked asset
                                p.BrandedPlaybackId = await _videoService.CreateBrandedAssetAsync(dbClip.MuxAssetId, logo, $"purchase:{p.Id}");
                                _logger.LogInformation($"[GIF-BRANDING] Triggered for Purchase {p.Id}, Clip {dbClip.Id}. Branded PID: {p.BrandedPlaybackId}");
                            }
                        }
                        catch (Exception gifEx)
                        {
                            _logger.LogError(gifEx, $"[GIF-BRANDING-ERROR] {gifEx.Message}");
                        }
                    }
                    // --- BRANDING LOGIC END ---
                }
                else
                {
                    // Even if clip not found in DB (edge case), ensure we don't carry over metadata filenames strictly
                    p.ClipMasterFileName = null;
                    p.ClipThumbnailFileName = null;
                }

                // DETACH CLIP BEFORE SAVE:
                // p.Clip comes from _clipRepository (Context A). _purchaseRepository uses a new Context B.
                // If we leave p.Clip attached, Context B will try to insert it as a new row, causing "Duplicate Key" on PK_Events.
                var tempClip = p.Clip;
                p.Clip = null;

                await _purchaseRepository.AddAsync(p);

                // RESTORE CLIP:
                // We need p.Clip back so the Email Service (later in this method) can read Title/Thumbnail.
                p.Clip = tempClip;
            }
        }

        // 3. Clear Cart (Only if running in user context, safer to try-catch)
        try
        {
            // Only applicable if we have a circuit to the active user's cart session
            // In a webhook, this won't clear the user's browser cart, but that's acceptable.
            // The CheckoutSuccess page will handle clearing via the service if called from frontend.
            if (userId != null)
            {
                // CartService usually relies on HttpContext or LocalStorage, which might not work in Webhook.
                // We will skip this here and let the Frontend call explicit Clear if necessary, 
                // or rely on the Fact that FulfillOrderAsync is called from CheckoutSuccess.
            }
        }
        catch { }

        // 4. Handle Promo Code Usage
        var promoId = await _paymentService.GetPromoCodeIdFromSessionAsync(sessionId);
        if (promoId.HasValue)
        {
            await _promoCodeRepository.IncrementUsageAsync(promoId.Value);
            var promo = await _promoCodeRepository.GetByIdAsync(promoId.Value);
            await _auditService.LogActionAsync(
                userId,
                null,
                "Apply Promo Code",
                "PromoCode",
                promoId.Value.ToString(),
                $"Code: {promo?.Code ?? "Unknown"} used in order {sessionId}");
        }

        // 5. Send Branded Email Receipt
        try
        {
            var storeName = await _storeSettingsService.GetStoreNameAsync();
            var subject = $"{storeName ?? "Project65"} Order Receipt (#{shortOrderId})";
            var customerEmail = await _paymentService.GetCustomerEmailFromSessionAsync(sessionId) ?? "customer@example.com";
            var customerName = purchases.FirstOrDefault()?.CustomerName ?? "Customer";

            var htmlBody = await _emailTemplateService.GenerateOrderReceiptHtmlAsync(shortOrderId, purchases, customerName);
            var textBody = await _emailTemplateService.GenerateOrderReceiptTextAsync(shortOrderId, purchases, customerName);
            await _emailService.SendEmailAsync(customerEmail, subject, htmlBody, textBody);

            // AUTO-SEND Fulfillment Email for GIF-only orders (they are auto-fulfilled)
            if (purchases.All(p => p.FulfillmentStatus == FulfillmentStatus.Fulfilled))
            {
                _logger.LogInformation($"[OrderFulfillment] All items fulfilled (GIF-only or Instant). Sending Fulfillment Email for {shortOrderId}");
                var fulfillSubject = $"{storeName ?? "Project65"} - Your Order is Ready! (#{shortOrderId})";
                var fulfillHtml = await _emailTemplateService.GenerateFulfillmentEmailHtmlAsync(shortOrderId, purchases, customerName);
                var fulfillText = await _emailTemplateService.GenerateFulfillmentTextAsync(shortOrderId, purchases, customerName);
                await _emailService.SendEmailAsync(customerEmail, fulfillSubject, fulfillHtml, fulfillText);
            }
            else
            {
                 _logger.LogInformation($"[OrderFulfillment] Order {shortOrderId} contains pending items. Skipping immediate fulfillment email.");
            }
        }
        catch (Exception ex)
        {
            // Log the error
            await _auditService.LogActionAsync(
                userId,
                null,
                "Email Failed",
                "Email",
                sessionId,
                $"Error: {ex.Message}");
        }

        return purchases;
    }
}
