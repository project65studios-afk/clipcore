using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface ISearchService
{
    Task<List<Clip>> SearchAsync(string query);
    void RefreshIndex();
}
