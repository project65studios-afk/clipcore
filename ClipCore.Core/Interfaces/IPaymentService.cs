using ClipCore.Core.DTOs;

namespace ClipCore.Core.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(IEnumerable<CheckoutItem> items, string successUrl, string cancelUrl, string? userEmail = null, string? userId = null, int? promoCodeId = null, Guid? tenantId = null, string? connectedAccountId = null);
    
    Task<List<ClipCore.Core.Entities.Purchase>> GetPurchasesFromSessionAsync(string sessionId);
    Task<string?> GetCustomerEmailFromSessionAsync(string sessionId);
    Task<int?> GetPromoCodeIdFromSessionAsync(string sessionId);

    // OAuth Onboarding
    Task<string> GetConnectOAuthUrlAsync(string redirectUri, string state, string? suggestedUrl = null, string? businessName = null, string? productDescription = null, string? email = null);
    Task<string> OnboardTenantAsync(string authorizationCode);
}
