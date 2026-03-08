using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(string id);
    Task<List<Collection>> ListAsync();
    Task<List<Collection>> SearchAsync(string query);
    Task AddAsync(Collection coll);
    Task UpdateAsync(Collection coll); // For adding clips, modifying summary
    Task DeleteAsync(string id);
}
