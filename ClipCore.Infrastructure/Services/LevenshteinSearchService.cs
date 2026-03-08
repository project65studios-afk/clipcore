using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using System.Collections.Concurrent;

namespace ClipCore.Infrastructure.Services;

public class LevenshteinSearchService : ISearchService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private ConcurrentDictionary<string, (string Title, string Tags)> _searchCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LevenshteinSearchService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<Clip>> SearchAsync(string query)
    {
        await EnsureCacheLoadedAsync();

        // 1. Exact / Substring Match (Run against DB for latest data)
        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IClipRepository>();
            var exactMatches = await repository.SearchAsync(query);

            // If we have enough exact matches, return them immediately
            if (exactMatches.Count >= 5)
            {
                return exactMatches;
            }

            // 2. Fuzzy Match (Run against Cache)
            var fuzzyMatchIds = PerformFuzzySearch(query);

            // 3. Fetch Fuzzy Entities
            // Exclude already found items
            var existingIds = exactMatches.Select(c => c.Id).ToHashSet();
            var newIds = fuzzyMatchIds.Where(id => !existingIds.Contains(id)).ToList();

            if (newIds.Any())
            {
                // We need to fetch these specific clips. 
                // Repository might not have a GetByIds method, so we might need to iterate or add one to repo.
                // For now, let's fetch individual since it's likely a small list (top 5-10).
                foreach (var id in newIds)
                {
                    var clip = await repository.GetByIdAsync(id);
                    if (clip != null)
                    {
                        exactMatches.Add(clip);
                    }
                }
            }

            return exactMatches;
        }
    }

    public void RefreshIndex()
    {
        // Fire and forget refresh
        _ = EnsureCacheLoadedAsync(force: true);
    }

    private List<string> PerformFuzzySearch(string query)
    {
        var candidates = new List<(string Id, int Score)>();
        var lowerQuery = query.ToLower();

        foreach (var item in _searchCache)
        {
            // Check Title
            var titleScore = Fuzz.PartialRatio(lowerQuery, item.Value.Title.ToLower());
            
            // Check Tags (simple join)
            var tagsScore = Fuzz.PartialRatio(lowerQuery, item.Value.Tags.ToLower());

            var maxScore = Math.Max(titleScore, tagsScore);

            // Threshold: 70 seems like a good starting point for "somewhat matches"
            if (maxScore > 70)
            {
                candidates.Add((item.Key, maxScore));
            }
        }

        return candidates
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => x.Id)
            .ToList();
    }

    private async Task EnsureCacheLoadedAsync(bool force = false)
    {
        if (!force && _searchCache.Any() && DateTime.UtcNow - _lastRefresh < _refreshInterval)
        {
            return;
        }

        try
        {
            await _lock.WaitAsync();

            if (!force && _searchCache.Any() && DateTime.UtcNow - _lastRefresh < _refreshInterval)
            {
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClipCore.Infrastructure.Data.AppDbContext>();
                
                // Fetch lightweight projection
                var items = context.Clips
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.Title, c.TagsJson })
                    .ToList();

                _searchCache.Clear();
                foreach (var item in items)
                {
                    _searchCache[item.Id] = (item.Title, item.TagsJson ?? "");
                }

                _lastRefresh = DateTime.UtcNow;
            }
        }
        catch
        {
            // Log error? For now, we just swallow to avoid crashing runtime search
        }
        finally
        {
            _lock.Release();
        }
    }
}
