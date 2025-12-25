using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(IEnumerable<Clip> clips, string successUrl, string cancelUrl, string? userEmail = null);
    Task<List<Project65.Core.Entities.Purchase>> GetPurchasesFromSessionAsync(string sessionId);
    Task<string?> GetCustomerEmailFromSessionAsync(string sessionId);
    // Needed for validating webhooks later
}
