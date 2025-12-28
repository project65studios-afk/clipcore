using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(string id);
    Task<List<Event>> ListAsync();
    Task<List<Event>> SearchAsync(string query);
    Task AddAsync(Event evt);
    Task UpdateAsync(Event evt); // For adding clips, modifying summary
    Task DeleteAsync(string id);
}
