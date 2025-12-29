using Project65.Core.DTOs;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;

namespace Project65.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly string _apiKey;

    public StripePaymentService(IConfiguration configuration)
    {
        _apiKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe:SecretKey");
        StripeConfiguration.ApiKey = _apiKey; // Global setting, simpler for MVP
    }

    public async Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null, string? userId = null)
    {
        var itemsList = items.ToList();
        Console.WriteLine($"[STRIPE] Creating Session for {itemsList.Count} items.");
        
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
                            { "ThumbnailFileName", item.ThumbnailFileName ?? "" }
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

        // Add backup snapshots to session metadata (up to limit of 50 keys)
        for (int i = 0; i < Math.Min(itemsList.Count, 20); i++) // Increased limit
        {
            var item = itemsList[i];
            sessionMetadata[$"c{i}_id"] = item.Id;
            sessionMetadata[$"c{i}_ev"] = item.EventName ?? "";
            sessionMetadata[$"c{i}_dt"] = item.EventDate?.ToString("yyyy-MM-dd") ?? "";
            sessionMetadata[$"c{i}_st"] = item.ClipRecordingStartedAt?.ToString("o") ?? "";
            sessionMetadata[$"c{i}_du"] = item.DurationSec?.ToString("F2") ?? "";
            sessionMetadata[$"c{i}_mf"] = item.MasterFileName ?? "";
            sessionMetadata[$"c{i}_tn"] = item.ThumbnailFileName ?? "";
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

        // Collect Phone Number via Custom Field (appears at bottom)
        options.CustomFields = new List<SessionCustomFieldOptions>
        {
            new SessionCustomFieldOptions
            {
                Key = "contact_number", // Changed key to avoid auto-mapping
                Label = new SessionCustomFieldLabelOptions 
                { 
                    Type = "custom", 
                    Custom = "Mobile / Contact Number" 
                },
                Type = "text",
                Optional = false
            }
        };

        // Disable standard top-level phone collection
        options.PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
        {
            Enabled = false,
        };
        
        // Collect Billing Address (auto-enabled by default usually, but we can enforce it)
        options.BillingAddressCollection = "required";

        var service = new SessionService();
        try 
        {
            Session session = await service.CreateAsync(options);
            return session.Url;
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[STRIPE ERROR] Failed to create session: {ex.Message}");
            Console.WriteLine($"[STRIPE ERROR] Type: {ex.StripeError?.Type}, Code: {ex.StripeError?.Code}");
            throw; // Re-throw to show error page or handle upstream
        }
    }

    public async Task<List<Purchase>> GetPurchasesFromSessionAsync(string sessionId)
    {
        var service = new SessionService();
        var options = new SessionGetOptions();
        options.AddExpand("line_items");
        options.AddExpand("line_items.data.price.product"); // Necessary to access Product Metadata
        
        var session = await service.GetAsync(sessionId, options);
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
                            if (string.IsNullOrEmpty(clipId)) clipId = session.Metadata.GetValueOrDefault($"c{index}_id");
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
                    if (DateTime.TryParse(clipStartedStr, out var cs)) clipStartedAt = cs;

                    double? durationSec = null;
                    if (double.TryParse(durationStr, out var ds)) durationSec = ds;
                    
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
                            ClipThumbnailFileName = thumbFile
                        };
                        
                        if (!foundClipIds.Contains(clipId))
                        {
                            purchases.Add(purchase);
                            foundClipIds.Add(clipId);
                        }
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
                        PricePaidCents = (int)((session.AmountTotal ?? 0) / (expectedClipIds.Count > 0 ? expectedClipIds.Count : 1)) 
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
        var session = await service.GetAsync(sessionId);
        return session.CustomerDetails?.Email ?? session.CustomerEmail;
    }
}
