using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(Clip clip, string successUrl, string cancelUrl);
    Task<Project65.Core.Entities.Purchase?> GetPurchaseFromSessionAsync(string sessionId); // Helper to verify and Create Purchase object
    // Needed for validating webhooks later
}
