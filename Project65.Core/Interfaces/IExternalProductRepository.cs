using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IExternalProductRepository
{
    Task<List<ExternalProduct>> GetAllAsync();
    Task<ExternalProduct?> GetByIdAsync(string id);
    Task AddAsync(ExternalProduct product);
    Task UpdateAsync(ExternalProduct product);
    Task DeleteAsync(string id);
}
