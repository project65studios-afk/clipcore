using ClipCore.API.Models.Clip;

namespace ClipCore.API.Interfaces;

public interface IClipData
{
    Task<IEnumerable<ClipItem>> GetClipsBySeller(int sellerId);
    Task<IEnumerable<ClipItem>> GetClipsByCollection(string collectionId);
    Task<ClipDetail?> GetClipDetail(string clipId);
    Task<ClipDetail?> GetClipDetailForSeller(string clipId, int sellerId);
    Task<IEnumerable<ClipItem>> SearchAsync(string query);
    Task<string> CreateClip(int sellerId, CreateClipRequest request);
    Task UpdateClip(int sellerId, UpdateClipRequest request);
    Task UpdateBatchSettings(string collectionId, int sellerId, int priceCents, int priceCommercialCents, bool allowGif, int gifPriceCents);
    Task DeleteClip(string clipId, int sellerId);
    Task SetMuxData(string clipId, string muxAssetId, string playbackIdSigned, string? playbackIdTeaser, double? durationSec, int? width, int? height);
    Task SetMuxUploadId(string clipId, string muxUploadId);
    Task ArchiveClip(string clipId);
    Task UpdateLastSoldAt(string clipId);
    Task<IEnumerable<ArchiveCandidateClip>> GetArchiveCandidates(int daysSinceLastSale);
}
