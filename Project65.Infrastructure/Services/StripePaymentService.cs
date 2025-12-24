using Microsoft.Extensions.Configuration;
using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Project65.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly string _apiKey;

    public StripePaymentService(IConfiguration configuration)
    {
        _apiKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe:SecretKey");
        StripeConfiguration.ApiKey = _apiKey; // Global setting, simpler for MVP
    }

    public async Task<string> CreateCheckoutSessionAsync(Clip clip, string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = clip.PriceCents,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = clip.Title,
                            Description = $"Video Clip: {clip.Title}",
                            // Metadata could go here
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "ClipId", clip.Id },
                { "EventId", clip.EventId }
            }
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task<Purchase?> GetPurchaseFromSessionAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);

        if (session.PaymentStatus == "paid")
        {
            // Extract metadata
            if (session.Metadata.TryGetValue("ClipId", out var clipId))
            {
                 return new Purchase
                 {
                     ClipId = clipId,
                     StripeSessionId = session.Id,
                     // UserId will be handled by the caller or passed in metadata? 
                     // For now, caller sets UserId.
                 };
            }
        }
        return null;
    }
}
