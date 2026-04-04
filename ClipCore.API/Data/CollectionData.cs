using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
using ClipCore.API.Models.Collection;

namespace ClipCore.API.Data;

public class CollectionData : ICollectionData
{
    private readonly ISqlDataAccess _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public CollectionData(ISqlDataAccess db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    public Task<IEnumerable<CollectionItem>> GetCollectionsBySeller(int sellerId) =>
        _db.LoadData<CollectionItem, dynamic>("SELECT * FROM cc_s_collections_by_seller(@SellerId)", new { SellerId = sellerId });

    public async Task<CollectionDetail?> GetCollectionDetail(string collectionId, int sellerId)
    {
        var coll = await _db.LoadSingle<CollectionDetail, dynamic>(
            @"SELECT c.""Id"", c.""Name"", c.""Date"", c.""Location"", c.""Summary"",
                     c.""DefaultPriceCents"", c.""DefaultPriceCommercialCents"",
                     c.""DefaultAllowGifSale"", c.""DefaultGifPriceCents"",
                     c.""HeroClipId"", c.""CreatedAt"",
                     COUNT(cl.""Id"")::int AS ""ClipCount""
              FROM ""Collections"" c
              LEFT JOIN ""Clips"" cl ON cl.""CollectionId"" = c.""Id"" AND cl.""IsArchived"" = false
              WHERE c.""Id"" = @CollectionId AND c.""SellerId"" = @SellerId GROUP BY c.""Id""",
            new { CollectionId = collectionId, SellerId = sellerId });

        if (coll is null) return null;

        coll.Clips = (await _db.LoadData<ClipItem, dynamic>(
            "SELECT * FROM cc_s_clips_by_collection(@CollectionId)",
            new { CollectionId = collectionId })).ToList();

        return coll;
    }

    public async Task<string> CreateCollection(int sellerId, CreateCollectionRequest r)
    {
        var id = Guid.NewGuid().ToString();
        await _db.SaveData(
            "SELECT cc_i_collection(@Id, @SellerId, @Name, @Date, @Location, @Summary, @DefaultPriceCents, @DefaultPriceCommercialCents, @DefaultAllowGifSale, @DefaultGifPriceCents)",
            new { Id = id, SellerId = sellerId, r.Name, r.Date, r.Location, r.Summary, r.DefaultPriceCents, r.DefaultPriceCommercialCents, r.DefaultAllowGifSale, r.DefaultGifPriceCents });
        return id;
    }

    public Task UpdateCollection(int sellerId, UpdateCollectionRequest r) =>
        _db.SaveData(
            "CALL cc_u_collection(@CollectionId, @SellerId, @Name, @Date, @Location, @Summary, @DefaultPriceCents, @DefaultPriceCommercialCents, @DefaultAllowGifSale, @DefaultGifPriceCents, @HeroClipId)",
            new { CollectionId = r.CollectionId, SellerId = sellerId, r.Name, r.Date, r.Location, r.Summary, r.DefaultPriceCents, r.DefaultPriceCommercialCents, r.DefaultAllowGifSale, r.DefaultGifPriceCents, r.HeroClipId });

    public async Task DeleteCollection(string collectionId, int sellerId)
    {
        var clips = await _db.LoadData<ClipAssets, dynamic>(
            "SELECT * FROM cc_s_collection_clip_assets(@CollectionId, @SellerId)",
            new { CollectionId = collectionId, SellerId = sellerId });

        // Resolve scoped Mux/R2 services — CollectionData is Singleton
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mux = scope.ServiceProvider.GetRequiredService<IMuxService>();
        var r2  = scope.ServiceProvider.GetRequiredService<IR2StorageService>();

        await Task.WhenAll(clips.SelectMany(c =>
        {
            var tasks = new List<Task>();
            if (!string.IsNullOrEmpty(c.MuxAssetId))        tasks.Add(Safe(() => mux.DeleteAssetAsync(c.MuxAssetId)));
            if (!string.IsNullOrEmpty(c.ThumbnailFileName)) tasks.Add(Safe(() => r2.DeleteAsync(c.ThumbnailFileName)));
            if (!string.IsNullOrEmpty(c.MasterFileName))    tasks.Add(Safe(() => r2.DeleteAsync(c.MasterFileName)));
            return tasks;
        }));

        await _db.SaveData("CALL cc_d_collection(@CollectionId, @SellerId)", new { CollectionId = collectionId, SellerId = sellerId });
    }

    private static async Task Safe(Func<Task> action)
    {
        try { await action(); } catch { }
    }

    private class ClipAssets
    {
        public string  Id { get; set; } = "";
        public string? MuxAssetId { get; set; }
        public string? ThumbnailFileName { get; set; }
        public string? MasterFileName { get; set; }
    }
}
