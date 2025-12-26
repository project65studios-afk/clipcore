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

    public async Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null)
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
                        Metadata = new Dictionary<string, string> { { "ClipId", item.Id } }
                    },
                },
                Quantity = 1,
            });
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "ClipIds", string.Join(",", clipIds) }
            },
            InvoiceCreation = new SessionInvoiceCreationOptions { Enabled = true }
        };

        if (!string.IsNullOrEmpty(userEmail))
        {
            options.CustomerEmail = userEmail;
        }

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return session.Url;
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

            // 1. Attempt to gather purchases from Line Items (Preferred Source)
            if (session.LineItems?.Data != null)
            {
                foreach (var item in session.LineItems.Data)
                {
                    var clipId = item.Price?.Product?.Metadata?.GetValueOrDefault("ClipId");
                    
                    if (!string.IsNullOrEmpty(clipId))
                    {
                        var purchase = new Purchase
                        {
                            ClipId = clipId,
                            StripeSessionId = session.Id,
                            CreatedAt = DateTime.UtcNow,
                            CustomerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail,
                            CustomerName = session.CustomerDetails?.Name,
                            CustomerAddress = addressStr,
                            PricePaidCents = Convert.ToInt32(item.AmountTotal)
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
            // This ensures that even if Line Item parsing fails (e.g. expansion limits), 
            // we still record the purchase if it was in the "ClipIds" list.
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
                        StripeSessionId = session.Id,
                        CreatedAt = DateTime.UtcNow,
                        CustomerEmail = session.CustomerDetails?.Email ?? session.CustomerEmail,
                        CustomerName = session.CustomerDetails?.Name,
                        CustomerAddress = addressStr,
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
