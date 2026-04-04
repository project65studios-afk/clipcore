using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;

namespace ClipCore.API.Data;

public class ClipData : IClipData
{
    private readonly ISqlDataAccess _db;
    public ClipData(ISqlDataAccess db) => _db = db;

    public Task<IEnumerable<ClipItem>> GetClipsBySeller(int sellerId) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_by_seller(@SellerId)", new { SellerId = sellerId });

    public Task<IEnumerable<ClipItem>> GetClipsByCollection(string collectionId) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_by_collection(@CollectionId)", new { CollectionId = collectionId });

    public Task<ClipDetail?> GetClipDetail(string clipId) =>
        _db.LoadSingle<ClipDetail, dynamic>("SELECT * FROM cc_s_clip_detail(@ClipId)", new { ClipId = clipId });

    public Task<ClipDetail?> GetClipDetailForSeller(string clipId, int sellerId) =>
        _db.LoadSingle<ClipDetail, dynamic>(
            @"SELECT c.*, col.""Name"" AS ""CollectionName"" FROM ""Clips"" c
              JOIN ""Collections"" col ON col.""Id"" = c.""CollectionId""
              WHERE c.""Id"" = @ClipId AND c.""SellerId"" = @SellerId",
            new { ClipId = clipId, SellerId = sellerId });

    public Task<IEnumerable<ClipItem>> SearchAsync(string query) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_search(@Query)", new { Query = query });

    public async Task<string> CreateClip(int sellerId, CreateClipRequest r)
    {
        var id = Guid.NewGuid().ToString();
        await _db.SaveData(
            "SELECT cc_i_clip(@Id, @CollectionId, @SellerId, @Title, @PriceCents, @PriceCommercialCents, @AllowGifSale, @GifPriceCents, @TagsJson)",
            new { Id = id, r.CollectionId, SellerId = sellerId, r.Title, r.PriceCents, r.PriceCommercialCents, r.AllowGifSale, r.GifPriceCents, r.TagsJson });
        return id;
    }

    public Task UpdateClip(int sellerId, UpdateClipRequest r) =>
        _db.SaveData(
            "CALL cc_u_clip(@ClipId, @SellerId, @Title, @PriceCents, @PriceCommercialCents, @AllowGifSale, @GifPriceCents, @TagsJson)",
            new { ClipId = r.ClipId, SellerId = sellerId, r.Title, r.PriceCents, r.PriceCommercialCents, r.AllowGifSale, r.GifPriceCents, r.TagsJson });

    public Task UpdateBatchSettings(string collectionId, int sellerId, int priceCents, int priceCommercialCents, bool allowGif, int gifPriceCents) =>
        _db.SaveData(
            "CALL cc_u_clip_batch_settings(@CollectionId, @SellerId, @PriceCents, @PriceCommercialCents, @AllowGif, @GifPriceCents)",
            new { CollectionId = collectionId, SellerId = sellerId, PriceCents = priceCents, PriceCommercialCents = priceCommercialCents, AllowGif = allowGif, GifPriceCents = gifPriceCents });

    public Task DeleteClip(string clipId, int sellerId) =>
        _db.SaveData("CALL cc_d_clip(@ClipId, @SellerId)", new { ClipId = clipId, SellerId = sellerId });

    public Task SetMuxData(string clipId, string muxAssetId, string playbackIdSigned, string? playbackIdTeaser, double? durationSec, int? width, int? height) =>
        _db.SaveData(
            "CALL cc_u_clip_mux_data(@ClipId, @MuxAssetId, @PlaybackIdSigned, @PlaybackIdTeaser, @DurationSec, @Width, @Height)",
            new { ClipId = clipId, MuxAssetId = muxAssetId, PlaybackIdSigned = playbackIdSigned, PlaybackIdTeaser = playbackIdTeaser, DurationSec = durationSec, Width = width, Height = height });

    public Task SetMuxUploadId(string clipId, string muxUploadId) =>
        _db.SaveData("CALL cc_u_clip_mux_upload_id(@ClipId, @MuxUploadId)", new { ClipId = clipId, MuxUploadId = muxUploadId });

    public Task ArchiveClip(string clipId) =>
        _db.SaveData("CALL cc_u_clip_archive(@ClipId)", new { ClipId = clipId });

    public Task UpdateLastSoldAt(string clipId) =>
        _db.SaveData("CALL cc_u_clip_last_sold(@ClipId)", new { ClipId = clipId });

    public Task<IEnumerable<ArchiveCandidateClip>> GetArchiveCandidates(int days) =>
        _db.LoadData<ArchiveCandidateClip, dynamic>("SELECT * FROM cc_s_archive_candidates(@Days)", new { Days = days });
}
