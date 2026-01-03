namespace ClipCore.Core.Interfaces;

public interface IVideoService
{
    Task<(string url, string uploadId)> CreateUploadUrlAsync(string clipId, string title, string? creatorId = null);
    Task<(string url, string uploadId)> CreateFulfillmentUploadUrlAsync(int purchaseId);
    Task<string?> GetPlaybackIdAsync(string assetId);
    Task<string?> EnsurePlaybackIdAsync(string assetId);
    Task<string> GetPlaybackToken(string playbackId, string audience = "v", string? maxResolution = null);
    Task DeleteAssetAsync(string assetId);
    Task<string?> GetAssetIdFromUploadAsync(string uploadId);
    Task<double?> GetAssetDurationAsync(string assetId);
    Task<(double? duration, DateTime? startedAt)> GetAssetDetailsAsync(string assetId);
    Task<string?> GetDownloadUrlAsync(string assetId, string? fileName = null);
}
