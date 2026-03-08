using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface ISearchService
{
    Task<List<Clip>> SearchAsync(string query);
    void RefreshIndex();
}
