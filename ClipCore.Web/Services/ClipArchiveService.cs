using ClipCore.Core.Interfaces;

namespace ClipCore.Web.Services;

/// <summary>
/// Daily background service that archives clips with no sales in 90 days.
/// Deletes the Mux asset (cost saving) but keeps the R2 master file intact.
/// Archived clips can be restored on demand by re-uploading from R2.
/// </summary>
public class ClipArchiveService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClipArchiveService> _logger;

    // Run once per day
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    // Archive clips unsold for 90 days
    private const int ArchiveDays = 90;

    public ClipArchiveService(IServiceProvider serviceProvider, ILogger<ClipArchiveService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 30 minutes after startup before first run to avoid startup contention
        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunArchivePassAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClipArchive] Archive pass failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunArchivePassAsync(CancellationToken ct)
    {
        _logger.LogInformation("[ClipArchive] Starting archive pass (threshold: {Days} days)", ArchiveDays);

        using var scope = _serviceProvider.CreateScope();
        var clipRepo = scope.ServiceProvider.GetRequiredService<IClipRepository>();
        var videoService = scope.ServiceProvider.GetRequiredService<IVideoService>();

        var candidates = await clipRepo.ListForArchiveAsync(ArchiveDays);

        _logger.LogInformation("[ClipArchive] Found {Count} clip(s) eligible for archiving", candidates.Count);

        var archived = 0;
        var failed = 0;

        foreach (var clip in candidates)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                if (!string.IsNullOrEmpty(clip.MuxAssetId))
                {
                    await videoService.DeleteAssetAsync(clip.MuxAssetId);
                }
                await clipRepo.ArchiveAsync(clip.Id);
                archived++;
                _logger.LogInformation("[ClipArchive] Archived clip {ClipId} ({Title})", clip.Id, clip.Title);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "[ClipArchive] Failed to archive clip {ClipId}", clip.Id);
            }
        }

        _logger.LogInformation("[ClipArchive] Pass complete: {Archived} archived, {Failed} failed", archived, failed);
    }
}
