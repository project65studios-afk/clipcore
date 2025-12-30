using Project65.Core.Entities;
using Project65.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Project65.Web.Hubs;

namespace Project65.Web.Services;

/// <summary>
/// Service responsible for background monitoring and "healing" of video clips 
/// that are in a processing state (missing PlaybackId or metadata).
/// </summary>
public class VideoHealingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ProcessingHub> _hubContext;
    private readonly ConcurrentDictionary<string, Task> _activeHealingTasks = new();

    /// <summary>
    /// Event fired when a clip has been successfully healed (PlaybackId and Metadata resolved).
    /// </summary>
    public event Action<Clip>? OnClipHealed;

    public VideoHealingService(IServiceScopeFactory scopeFactory, IHubContext<ProcessingHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Starts background healing tasks for any "broken" clips in the provided list.
    /// </summary>
    public void StartHealingForClips(IEnumerable<Clip> clips)
    {
        var brokenClips = clips.Where(c => 
            (string.IsNullOrEmpty(c.PlaybackIdSigned) && string.IsNullOrEmpty(c.PlaybackIdTeaser) && !string.IsNullOrEmpty(c.MuxUploadId))
            || (!c.RecordingStartedAt.HasValue && !string.IsNullOrEmpty(c.MuxAssetId))
        ).ToList();

        if (!brokenClips.Any()) return;

        foreach (var clip in brokenClips)
        {
            if (_activeHealingTasks.TryAdd(clip.Id, Task.Run(() => HealClipLoopAsync(clip.Id))))
            {
                // Task is already started by TryAdd, no need for separate Task.Run
            }
        }
    }

    private async Task HealClipLoopAsync(string clipId)
    {
        try
        {
            // Try for up to 3 minutes (36 iterations with backoff ≈ 180s)
            for (int i = 0; i < 36; i++)
            {
                using var scope = _scopeFactory.CreateScope();
                var clipRepo = scope.ServiceProvider.GetRequiredService<IClipRepository>();
                var videoService = scope.ServiceProvider.GetRequiredService<IVideoService>();
                
                var clip = await clipRepo.GetByIdAsync(clipId);
                if (clip == null) break;

                bool clipFixed = false;

                // 1. Check if it's already fixed in DB (by another process or previous iteration)
                if (!string.IsNullOrEmpty(clip.PlaybackIdSigned) && clip.RecordingStartedAt.HasValue)
                {
                    clipFixed = true;
                }
                else
                {
                    // 2. Active Repair via Mux API
                    // A. Resolve Asset ID if missing
                    if (string.IsNullOrEmpty(clip.MuxAssetId) && !string.IsNullOrEmpty(clip.MuxUploadId))
                    {
                        var assetId = await videoService.GetAssetIdFromUploadAsync(clip.MuxUploadId);
                        if (!string.IsNullOrEmpty(assetId))
                        {
                            clip.MuxAssetId = assetId;
                            await clipRepo.UpdateAsync(clip);
                        }
                    }

                    // B. Resolve Playback ID & Metadata if we have Asset ID
                    if (!string.IsNullOrEmpty(clip.MuxAssetId))
                    {
                        var playbackId = await videoService.EnsurePlaybackIdAsync(clip.MuxAssetId);
                        if (!string.IsNullOrEmpty(playbackId))
                        {
                            clip.PlaybackIdSigned = playbackId;
                            
                            var (duration, startedAt) = await videoService.GetAssetDetailsAsync(clip.MuxAssetId);
                            if (duration.HasValue) clip.DurationSec = duration;
                            if (startedAt.HasValue) clip.RecordingStartedAt = startedAt;

                            await clipRepo.UpdateAsync(clip);
                            clipFixed = true;
                        }
                    }
                }

                if (clipFixed)
                {
                    // Successfully healed
                    OnClipHealed?.Invoke(clip);
                    await _hubContext.Clients.All.SendAsync("ClipStatusUpdated", clip.Id, "Healed");
                    break;
                }

                // Exponential backoff or step-based
                int delay = i < 10 ? 1000 : (i < 20 ? 2000 : 5000);
                await Task.Delay(delay);
            }
        }
        catch (Exception)
        {
            // Error healing clip
        }
        finally
        {
            _activeHealingTasks.TryRemove(clipId, out _);
        }
    }
}
