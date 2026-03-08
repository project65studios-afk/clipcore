using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using ClipCore.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipCore.Infrastructure.Services.Fakes;

public class FakePaymentService : IPaymentService
{
    public Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null, string? userId = null, int? promoCodeId = null)
    {
        // Return a mock URL that starts with checkout.stripe.com to satisfy the smoke test requirement
        return Task.FromResult("https://checkout.stripe.com/pay/mock_session_123");
    }

    public Task<List<Purchase>> GetPurchasesFromSessionAsync(string sessionId)
    {
        return Task.FromResult(new List<Purchase>());
    }

    public Task<string?> GetCustomerEmailFromSessionAsync(string sessionId)
    {
        return Task.FromResult<string?>("test@example.com");
    }

    public Task<int?> GetPromoCodeIdFromSessionAsync(string sessionId)
    {
        return Task.FromResult<int?>(null);
    }
}
