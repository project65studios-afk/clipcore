using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface IExternalProductRepository
{
    Task<List<ExternalProduct>> GetAllAsync();
    Task<ExternalProduct?> GetByIdAsync(string id);
    Task AddAsync(ExternalProduct product);
    Task UpdateAsync(ExternalProduct product);
    Task DeleteAsync(string id);
}
