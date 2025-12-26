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
        var lineItems = new List<SessionLineItemOptions>();
        var clipIds = new List<string>();

        foreach (var item in items)
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
        var session = await service.GetAsync(sessionId);
        var purchases = new List<Purchase>();

        if (session.PaymentStatus == "paid")
        {
            // Try to get ClipIds from session metadata
            if (session.Metadata.TryGetValue("ClipIds", out var clipIdsStr))
            {
                var clipIds = clipIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in clipIds)
                {
                    purchases.Add(new Purchase
                    {
                        ClipId = id,
                        StripeSessionId = session.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            // Fallback for legacy single-item sessions
            else if (session.Metadata.TryGetValue("ClipId", out var singleClipId))
            {
                 purchases.Add(new Purchase
                 {
                     ClipId = singleClipId,
                     StripeSessionId = session.Id,
                     CreatedAt = DateTime.UtcNow
                 });
            }
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
