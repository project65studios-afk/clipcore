using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClipCore.Web.Services;

public class SummaryGenerationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVisionService _visionService;
    private readonly IVideoService _videoService;
    private readonly IStorageService _storageService;

    public SummaryGenerationService(
        IServiceScopeFactory scopeFactory, 
        IVisionService visionService,
        IVideoService videoService,
        IStorageService storageService)
    {
        _scopeFactory = scopeFactory;
        _visionService = visionService;
        _videoService = videoService;
        _storageService = storageService;
    }

    public async Task<string> GenerateEventSummaryAsync(string eventId)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        
        var evt = await eventRepo.GetByIdAsync(eventId);
        if (evt == null) return "Event not found.";

        var clips = evt.Clips.Take(5).ToList();
        if (!clips.Any()) return "No clips found for this event to analyze.";

        var imageUrls = new List<string>();

        foreach (var clip in clips)
        {
            var url = await GetClipThumbnailUrlAsync(clip);
            if (!string.IsNullOrEmpty(url))
            {
                imageUrls.Add(url);
            }
        }

        if (!imageUrls.Any()) return "Could not retrieve any clip thumbnails for analysis.";

        var summary = await _visionService.GenerateBatchSummaryAsync(imageUrls);
        
        if (!string.IsNullOrEmpty(summary) && !summary.StartsWith("Error"))
        {
            evt.Summary = summary;
            await eventRepo.UpdateAsync(evt);
        }

        return summary;
    }

    private async Task<string?> GetClipThumbnailUrlAsync(Clip clip)
    {
        // 1. Try R2 Thumbnail (Cloud-accessible URL)
        if (!string.IsNullOrEmpty(clip.ThumbnailFileName))
        {
            var storageKey = clip.ThumbnailFileName.Contains("/") 
                ? clip.ThumbnailFileName 
                : $"thumbnails/{clip.ThumbnailFileName}";
            return _storageService.GetPresignedDownloadUrl(storageKey);
        }

        // 2. Try Mux Snapshot
        var playbackId = clip.PlaybackIdTeaser ?? clip.PlaybackIdSigned;
        if (!string.IsNullOrEmpty(playbackId) && !playbackId.StartsWith("mock"))
        {
            var token = await _videoService.GetPlaybackToken(playbackId, "t");
            return $"https://image.mux.com/{playbackId}/thumbnail.jpg?token={token}&width=600";
        }

        return null;
    }
}
