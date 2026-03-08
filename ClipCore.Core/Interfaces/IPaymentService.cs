using ClipCore.Core.DTOs;

namespace ClipCore.Core.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null, string? userId = null, int? promoCodeId = null);
    Task<List<ClipCore.Core.Entities.Purchase>> GetPurchasesFromSessionAsync(string sessionId);
    Task<string?> GetCustomerEmailFromSessionAsync(string sessionId);
    Task<int?> GetPromoCodeIdFromSessionAsync(string sessionId);
}
