using ClipCore.Core.Entities;

namespace ClipCore.API.Interfaces;

public interface IPromoCodeData
{
    Task<PromoCode?> GetByCodeAsync(string code);
    Task<IEnumerable<PromoCode>> ListAsync();
    Task AddAsync(PromoCode promo);
    Task IncrementUsageAsync(int id);
    Task DeleteAsync(int id);
}
