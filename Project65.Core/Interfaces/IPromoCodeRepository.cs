using System.Collections.Generic;
using System.Threading.Tasks;
using Project65.Core.Entities;

namespace Project65.Core.Interfaces
{
    public interface IPromoCodeRepository
    {
        Task<PromoCode?> GetByIdAsync(int id);
        Task<PromoCode?> GetByCodeAsync(string code);
        Task<List<PromoCode>> ListAsync();
        Task AddAsync(PromoCode promoCode);
        Task UpdateAsync(PromoCode promoCode);
        Task DeleteAsync(int id);
        Task IncrementUsageAsync(int id);
    }
}
