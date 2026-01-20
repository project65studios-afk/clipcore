using Project65.Core.DTOs;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Project65.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly string _apiKey;
    private readonly Polly.ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _logger = logger;
        _apiKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe:SecretKey");
        StripeConfiguration.ApiKey = _apiKey; // Global setting, simpler for MVP
        
        // Define Polly Resilience Pipeline for Stripe
        _resiliencePipeline = new Polly.ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new Polly.PredicateBuilder().Handle<StripeException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = Polly.DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    // Can't access instance logger easily in static context without capturing, 
                    // but we can just use Console for fallback or restructure. 
                    // Better to just leave Console or capture logger if we change to non-static.
                    // For now, let's keep it simple as this is a delegate.
                    Console.WriteLine($"[STRIPE-RETRY] Retrying Stripe API call... (Attempt {args.AttemptNumber})");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null, string? userId = null, int? promoCodeId = null)
    {
        var itemsList = items.ToList();
        _logger.LogInformation($"[Stripe] Creating Session for {itemsList.Count} items. User: {userId ?? "Guest"}");
        
        var lineItems = new List<SessionLineItemOptions>();
        var clipIds = new List<string>();

        foreach (var item in itemsList)
        {
            clipIds.Add(item.Id);
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = item.PriceCents,
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Title,
                        Description = $"Video Clip: {item.Title}",
                        Metadata = new Dictionary<string, string> 
                        { 
                            { "ClipId", item.Id }, 
                            { "EventName", item.EventName ?? "" },
                            { "EventDate", item.EventDate?.ToString("yyyy-MM-dd") ?? "" },
                            { "ClipRecordingStartedAt", item.ClipRecordingStartedAt?.ToString("o") ?? "" },
                            { "DurationSec", item.DurationSec?.ToString("F2") ?? "" },
                            { "MasterFileName", item.MasterFileName ?? "" },
                            { "ThumbnailFileName", item.ThumbnailFileName ?? "" },
                            { "LicenseType", item.LicenseType.ToString() },
                            { "IsGif", item.IsGif.ToString() },
                            { "GifStartTime", item.GifStartTime?.ToString("F2") ?? "" },
                            { "GifEndTime", item.GifEndTime?.ToString("F2") ?? "" }
                        }
                    },
                },
                Quantity = 1,
            });
        }

        var sessionMetadata = new Dictionary<string, string>
        {
            { "ClipIds", string.Join(",", clipIds) }
        };

        if (promoCodeId.HasValue)
        {
            sessionMetadata["PromoCodeId"] = promoCodeId.Value.ToString();
        }

        // Add backup snapshots to session metadata (up to limit of 50 keys)
        for (int i = 0; i < Math.Min(itemsList.Count, 15); i++) // Reduced limit to fit new keys
        {
            var item = itemsList[i];
            sessionMetadata[$"c{i}_id"] = item.Id;
            sessionMetadata[$"c{i}_ev"] = item.EventName ?? "";
            sessionMetadata[$"c{i}_dt"] = item.EventDate?.ToString("yyyy-MM-dd") ?? "";
            sessionMetadata[$"c{i}_st"] = item.ClipRecordingStartedAt?.ToString("o") ?? "";
            sessionMetadata[$"c{i}_du"] = item.DurationSec?.ToString("F2") ?? "";
            sessionMetadata[$"c{i}_mf"] = item.MasterFileName ?? "";
            sessionMetadata[$"c{i}_tn"] = item.ThumbnailFileName ?? "";
            sessionMetadata[$"c{i}_li"] = item.LicenseType.ToString();
            if (item.IsGif)
            {
               sessionMetadata[$"c{i}_ig"] = "true";
               sessionMetadata[$"c{i}_gs"] = item.GifStartTime?.ToString("F2") ?? "";
               sessionMetadata[$"c{i}_ge"] = item.GifEndTime?.ToString("F2") ?? "";
            }
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = sessionMetadata,
            InvoiceCreation = new SessionInvoiceCreationOptions { Enabled = true },
            ClientReferenceId = userId
        };

        if (!string.IsNullOrEmpty(userEmail))
        {
            options.CustomerEmail = userEmail;
        }

        // Enable standard top-level phone collection (Autofill supported)
        options.PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
        {
            Enabled = true,
        };
        
        // Collect Billing Address (auto-enabled by default usually, but we can enforce it)
        options.BillingAddressCollection = "required";

        var service = new SessionService();
        try 
        {
            Session session = await _resiliencePipeline.ExecuteAsync(async ct => await service.CreateAsync(options, cancellationToken: ct));
            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"[Stripe] Failed to create session. Type: {ex.StripeError?.Type}, Code: {ex.StripeError?.Code}");
            throw; // Re-throw to show error page or handle upstream
        }
    }

    public async Task<List<Purchase>> GetPurchasesFromSessionAsync(string sessionId)
    {
        var service = new SessionService();
        var options = new SessionGetOptions();
        options.AddExpand("line_items");
        options.AddExpand("line_items.data.price.product"); // Necessary to access Product Metadata
        
        var session = await _resiliencePipeline.ExecuteAsync(async ct => await service.GetAsync(sessionId, options, cancellationToken: ct));
        var purchases = new List<Purchase>();
        var foundClipIds = new HashSet<string>();
        var userId = session.ClientReferenceId;

        if (session.PaymentStatus == "paid")
        {
            // Address Formatting
            var address = session.CustomerDetails?.Address;
            string? addressStr = null;
            if (address != null)
            {
                var parts = new[] { address.Line1, address.Line2, address.City, address.State, address.PostalCode, address.Country };
                addressStr = string.Join(", ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            
            // Phone Number Retrieval (From Custom Field)
            string? phone = null;
            if (session.CustomFields != null)
            {
                var phoneField = session.CustomFields.FirstOrDefault(CF => CF.Key == "contact_number");
                phone = phoneField?.Text?.Value;
            }
            // Fallback to customer detail if ever present
            if (string.IsNullOrEmpty(phone)) phone = session.CustomerDetails?.Phone;

            // 1. Attempt to gather purchases from Line Items (Preferred Source)
            if (session.LineItems?.Data != null)
            {
                foreach (var item in session.LineItems.Data)
                {
                    var product = item.Price?.Product;
                    var clipId = product?.Metadata?.GetValueOrDefault("ClipId");
                    var eventName = product?.Metadata?.GetValueOrDefault("EventName");
                    var eventDateStr = product?.Metadata?.GetValueOrDefault("EventDate");
                    var clipStartedStr = product?.Metadata?.GetValueOrDefault("ClipRecordingStartedAt");
                    var durationStr = product?.Metadata?.GetValueOrDefault("DurationSec");
                    var masterFile = product?.Metadata?.GetValueOrDefault("MasterFileName");
                    var thumbFile = product?.Metadata?.GetValueOrDefault("ThumbnailFileName");
                    var licenseTypeStr = product?.Metadata?.GetValueOrDefault("LicenseType");
                    var isGifStr = product?.Metadata?.GetValueOrDefault("IsGif");
                    var gifStartStr = product?.Metadata?.GetValueOrDefault("GifStartTime");
                    var gifEndStr = product?.Metadata?.GetValueOrDefault("GifEndTime");

                    // Backup lookup from session metadata (by index or by clipId)
                    // Stripe preserves order of line items, so we can use a counter
                    var index = session.LineItems.Data.IndexOf(item);
                    
                    if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(durationStr) || string.IsNullOrEmpty(masterFile))
                    {
                        // 1. Try match by ClipId if we have it
                        bool foundByClipId = false;
                        if (!string.IsNullOrEmpty(clipId))
                        {
                            for (int i = 0; i < 20; i++) // Check up to 20
                            {
                                if (session.Metadata.GetValueOrDefault($"c{i}_id") == clipId)
                                {
                                    if (string.IsNullOrEmpty(eventName)) eventName = session.Metadata.GetValueOrDefault($"c{i}_ev");
                                    if (string.IsNullOrEmpty(eventDateStr)) eventDateStr = session.Metadata.GetValueOrDefault($"c{i}_dt");
                                    if (string.IsNullOrEmpty(clipStartedStr)) clipStartedStr = session.Metadata.GetValueOrDefault($"c{i}_st");
                                    if (string.IsNullOrEmpty(durationStr)) durationStr = session.Metadata.GetValueOrDefault($"c{i}_du");
                                    if (string.IsNullOrEmpty(masterFile)) masterFile = session.Metadata.GetValueOrDefault($"c{i}_mf");
                                    if (string.IsNullOrEmpty(thumbFile)) thumbFile = session.Metadata.GetValueOrDefault($"c{i}_tn");
                                    if (string.IsNullOrEmpty(licenseTypeStr)) licenseTypeStr = session.Metadata.GetValueOrDefault($"c{i}_li");
                                    if (string.IsNullOrEmpty(isGifStr)) isGifStr = session.Metadata.GetValueOrDefault($"c{i}_ig");
                                    if (string.IsNullOrEmpty(gifStartStr)) gifStartStr = session.Metadata.GetValueOrDefault($"c{i}_gs");
                                    if (string.IsNullOrEmpty(gifEndStr)) gifEndStr = session.Metadata.GetValueOrDefault($"c{i}_ge");
                                    foundByClipId = true;
                                    break;
                                }
                            }
                        }

                        // 2. Fallback: Match by index (Stripe line items are sequential)
                        if (!foundByClipId && index >= 0 && index < 20)
                        {
                            if (string.IsNullOrEmpty(eventName)) eventName = session.Metadata.GetValueOrDefault($"c{index}_ev");
                            if (string.IsNullOrEmpty(eventDateStr)) eventDateStr = session.Metadata.GetValueOrDefault($"c{index}_dt");
                            if (string.IsNullOrEmpty(clipStartedStr)) clipStartedStr = session.Metadata.GetValueOrDefault($"c{index}_st");
                            if (string.IsNullOrEmpty(durationStr)) durationStr = session.Metadata.GetValueOrDefault($"c{index}_du");
                            if (string.IsNullOrEmpty(masterFile)) masterFile = session.Metadata.GetValueOrDefault($"c{index}_mf");
                            if (string.IsNullOrEmpty(thumbFile)) thumbFile = session.Metadata.GetValueOrDefault($"c{index}_tn");
                            if (string.IsNullOrEmpty(licenseTypeStr)) licenseTypeStr = session.Metadata.GetValueOrDefault($"c{index}_li");
                            if (string.IsNullOrEmpty(clipId)) clipId = session.Metadata.GetValueOrDefault($"c{index}_id");
                            if (string.IsNullOrEmpty(isGifStr)) isGifStr = session.Metadata.GetValueOrDefault($"c{index}_ig");
                            if (string.IsNullOrEmpty(gifStartStr)) gifStartStr = session.Metadata.GetValueOrDefault($"c{index}_gs");
                            if (string.IsNullOrEmpty(gifEndStr)) gifEndStr = session.Metadata.GetValueOrDefault($"c{index}_ge");
                        }
                    }

                    // Final Scrub: Treat empty strings as null for consistent persistence
                    if (string.IsNullOrEmpty(eventName)) eventName = null;
                    if (string.IsNullOrEmpty(eventDateStr)) eventDateStr = null;
                    if (string.IsNullOrEmpty(clipStartedStr)) clipStartedStr = null;
                    if (string.IsNullOrEmpty(durationStr)) durationStr = null;
                    if (string.IsNullOrEmpty(masterFile)) masterFile = null;
                    if (string.IsNullOrEmpty(thumbFile)) thumbFile = null;

                    DateOnly? eventDate = null;
                    if (DateOnly.TryParse(eventDateStr, out var ed)) eventDate = ed;
                    
                    DateTime? clipStartedAt = null;
                    if (DateTime.TryParse(clipStartedStr, out var cs)) 
                    {
                        if (cs.Kind == DateTimeKind.Unspecified) 
                        {
                            clipStartedAt = DateTime.SpecifyKind(cs, DateTimeKind.Utc);
                        }
                        else 
                        {
                            clipStartedAt = cs.ToUniversalTime();
                        }
                    }

                    double? durationSec = null;
                    if (double.TryParse(durationStr, out var ds)) durationSec = ds;

                    var licenseType = LicenseType.Personal;
                    if (Enum.TryParse<LicenseType>(licenseTypeStr, out var lt)) licenseType = lt;
                    
                    bool isGif = false;
                    if (bool.TryParse(isGifStr, out var ig)) isGif = ig;

                    double? gifStart = null;
                    if (double.TryParse(gifStartStr, out var gs)) gifStart = gs;

                    double? gifEnd = null;
                    if (double.TryParse(gifEndStr, out var ge)) gifEnd = ge;
                    
                    if (!string.IsNullOrEmpty(clipId))
                    {
                        var purchase = new Purchase
                        {
                            ClipId = clipId,
                            UserId = userId,
                            StripeSessionId = session.Id,
                            CreatedAt = DateTime.UtcNow,
                            CustomerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail,
                            CustomerName = session.CustomerDetails?.Name,
                            CustomerAddress = addressStr,
                            CustomerPhone = phone,
                            PricePaidCents = Convert.ToInt32(item.AmountTotal),
                            ClipTitle = item.Price?.Product?.Name ?? "Unknown Clip",
                            EventName = eventName,
                            EventDate = eventDate,
                            ClipRecordingStartedAt = clipStartedAt,
                            ClipDurationSec = durationSec,
                            ClipMasterFileName = masterFile,
                            ClipThumbnailFileName = thumbFile,
                            LicenseType = licenseType,
                            IsGif = isGif,
                            GifStartTime = gifStart,
                            GifEndTime = gifEnd
                        };
                        
                        if (!foundClipIds.Contains(clipId))
                        {
                            purchases.Add(purchase);
                            foundClipIds.Add(clipId);
                        }
                    }
                    else
                    {
                        var productName = item.Price?.Product?.Name ?? "Unknown";
                        Console.WriteLine($"[STRIPE-WARNING] Item '{productName}' (Index {index}) skipped. Missing ClipId. Metadata Count: {item.Price?.Product?.Metadata?.Count ?? 0}");
                    }
                }
            }

            // 2. Fallback / Fill Gaps using Session Metadata
            // ... (Metadata logic remains same, just adding phone mapping)
            var expectedClipIds = new List<string>();
            if (session.Metadata.TryGetValue("ClipIds", out var clipIdsStr))
            {
                expectedClipIds.AddRange(clipIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            else if (session.Metadata.TryGetValue("ClipId", out var singleClipId))
            {
                expectedClipIds.Add(singleClipId);
            }

            foreach (var expectedId in expectedClipIds)
            {
                if (!foundClipIds.Contains(expectedId))
                {
                    Console.WriteLine($"[STRIPE] Clip {expectedId} found in Metadata but NOT LineItems. Adding fallback purchase.");
                    purchases.Add(new Purchase
                    {
                        ClipId = expectedId,
                        UserId = userId,
                        StripeSessionId = session.Id,
                        CreatedAt = DateTime.UtcNow,
                        CustomerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail,
                        CustomerName = session.CustomerDetails?.Name,
                        CustomerAddress = addressStr,
                        CustomerPhone = phone,
                        // Fallback Price: Average of total
                        PricePaidCents = (int)((session.AmountTotal ?? 0) / (expectedClipIds.Count > 0 ? expectedClipIds.Count : 1)),
                        LicenseType = Enum.TryParse<LicenseType>(session.Metadata.GetValueOrDefault($"c{foundClipIds.Count}_li"), out var lt) ? lt : LicenseType.Personal
                    });
                    foundClipIds.Add(expectedId);
                }
            }
            
            Console.WriteLine($"[STRIPE] Processed session {sessionId}. Found {purchases.Count} purchases (Expected {expectedClipIds.Count})."); 
        }
        return purchases;
    }

    public async Task<string?> GetCustomerEmailFromSessionAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await _resiliencePipeline.ExecuteAsync(async ct => await service.GetAsync(sessionId, cancellationToken: ct));
        return session.CustomerDetails?.Email ?? session.CustomerEmail;
    }

    public async Task<int?> GetPromoCodeIdFromSessionAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await _resiliencePipeline.ExecuteAsync(async ct => await service.GetAsync(sessionId, cancellationToken: ct));
        if (session.Metadata.TryGetValue("PromoCodeId", out var promoIdStr) && int.TryParse(promoIdStr, out var promoId))
        {
            return promoId;
        }
        return null;
    }
}
