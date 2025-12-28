using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IClipRepository
{
    Task<Clip?> GetByIdAsync(string id);
    Task<List<Clip>> SearchAsync(string query);
    Task<List<Clip>> GetByEventIdAsync(string eventId);
    Task<List<Clip>> GetRelatedAsync(string eventId, string[] tags, string excludeClipId, int count = 4);
    Task AddAsync(Clip clip);
    Task UpdateAsync(Clip clip); // For updating processing status/metadata
}
