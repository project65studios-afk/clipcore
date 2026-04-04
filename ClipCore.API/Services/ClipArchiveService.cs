using ClipCore.API.Interfaces;

namespace ClipCore.API.Services;

// Runs daily at 2am UTC.
// Deletes Mux asset (saves streaming costs), keeps R2 master file.
public class ClipArchiveService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ClipArchiveService> _logger;

    public ClipArchiveService(IServiceProvider services, ILogger<ClipArchiveService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(2);
            await Task.Delay(nextRun - DateTime.UtcNow, ct);
            try { await ArchiveAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "ClipArchiveService failed"); }
        }
    }

    private async Task ArchiveAsync()
    {
        await using var scope    = _services.CreateAsyncScope();
        var clipData = scope.ServiceProvider.GetRequiredService<IClipData>();
        var mux      = scope.ServiceProvider.GetRequiredService<IMuxService>();

        var candidates = await clipData.GetArchiveCandidates(90);
        foreach (var clip in candidates)
        {
            try
            {
                if (!string.IsNullOrEmpty(clip.MuxAssetId))
                    await mux.DeleteAssetAsync(clip.MuxAssetId);
                await clipData.ArchiveClip(clip.Id);
                _logger.LogInformation("Archived clip {Id}", clip.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive clip {Id}", clip.Id);
            }
        }
    }
}
