using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IClipRepository
{
    Task<Clip?> GetByIdAsync(string id);
    Task AddAsync(Clip clip);
    Task UpdateAsync(Clip clip); // For updating processing status/metadata
}
