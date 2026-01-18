using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Services.Fakes;

public class FakeVideoService : IVideoService
{
    public Task<(string url, string uploadId)> CreateUploadUrlAsync(string clipId, string title, string? creatorId = null)
    {
        return Task.FromResult(("https://fake-mux.com/upload/123", "fake-upload-123"));
    }

    public Task<(string url, string uploadId)> CreateDirectUploadUrlAsync(string? title = null, string? creatorId = null, string? passthrough = null)
    {
        return Task.FromResult(("https://fake-mux.com/direct/upload/123", "fake-direct-123"));
    }

    public Task<(string url, string uploadId)> CreateFulfillmentUploadUrlAsync(int purchaseId)
    {
        return Task.FromResult(("https://fake-mux.com/upload/fulfillment/456", "fake-upload-456"));
    }

    public Task<string?> GetPlaybackIdAsync(string assetId)
    {
        return Task.FromResult<string?>("fake-playback-789");
    }

    public Task<string?> EnsurePlaybackIdAsync(string assetId)
    {
        return Task.FromResult<string?>("fake-playback-789");
    }

    public Task<string> GetPlaybackToken(string playbackId, string audience = "v", string? maxResolution = null)
    {
        return Task.FromResult($"fake-token-{playbackId}-{audience}");
    }

    public Task DeleteAssetAsync(string assetId)
    {
        return Task.CompletedTask;
    }

    public Task<string?> GetAssetIdFromUploadAsync(string uploadId)
    {
        return Task.FromResult<string?>("fake-asset-999");
    }

    public Task<double?> GetAssetDurationAsync(string assetId)
    {
        return Task.FromResult<double?>(120.5);
    }

    public Task<(double? duration, DateTime? startedAt)> GetAssetDetailsAsync(string assetId)
    {
        return Task.FromResult< (double?, DateTime?)>((120.5, DateTime.UtcNow.AddHours(-1)));
    }

    public string GetGifUrlAsync(string playbackId, double? start = null, double? duration = null)
    {
        return "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjEx.../giphy.gif"; // Funny placeholder
    }

    public Task<string?> GetDownloadUrlAsync(string assetId, string? fileName = null)
    {
        return Task.FromResult<string?>("https://fake-mux.com/download/fake-asset-999");
    }
    public Task<int> DeleteErroredAssetsAsync()
    {
        return Task.FromResult(0);
    }
}
