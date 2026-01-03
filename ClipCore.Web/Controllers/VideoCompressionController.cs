using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClipCore.Core.Interfaces;
using ClipCore.Core.Entities;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ClipCore.Web.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("admin")]
public class VideoCompressionController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly IClipRepository _clipRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IStorageService _storageService;
    private readonly IVisionService _visionService;
    private readonly ILogger<VideoCompressionController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public VideoCompressionController(
        IVideoService videoService,
        IClipRepository clipRepository,
        IEventRepository eventRepository,
        IStorageService storageService,
        IVisionService visionService,
        ILogger<VideoCompressionController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _videoService = videoService;
        _clipRepository = clipRepository;
        _eventRepository = eventRepository;
        _storageService = storageService;
        _visionService = visionService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpPost("compress-and-upload")]
    public async Task<IActionResult> CompressAndUpload(
        [FromForm] IFormFile file,
        [FromForm] string? eventId,
        [FromForm] string? masterFileName,
        [FromForm] int priceCents,
        [FromForm] string? userId,
        [FromForm] string? lastModified = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (string.IsNullOrEmpty(eventId))
                return BadRequest(new { error = "Event ID is required" });

            // Validate Event and Get TenantId (Early for R2 path construction)
            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                return BadRequest(new { error = "Event not found" });
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "video-compression", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Save uploaded file locally for ffmpeg
                var inputPath = Path.Combine(tempDir, file.FileName);
                using (var stream = new FileStream(inputPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"[Compression] Received {file.FileName}, size: {file.Length / 1024 / 1024}MB");

                var clipId = Guid.NewGuid().ToString();

                // Define Task 1: Upload Original to R2 (Master)
                // We use a separate stream from the IFormFile to avoid conflict, although file.OpenReadStream() creates a new stream.
                // However, to be safe and avoid reading the request stream concurrently if it's not buffered,
                // we will upload from the LOCAL FILE we just saved. This is safer and robust.
                var r2UploadTask = Task.Run(async () => 
                {
                    try 
                    {
                         // Structure: {TenantId}/events/{EventId}/clips/{FileName}
                         var r2Key = $"{evt.TenantId}/events/{evt.Id}/clips/{file.FileName}";
                         _logger.LogInformation($"[R2] Uploading master file to {r2Key}");
                         
                         using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                         {
                             await _storageService.UploadAsync(fs, r2Key, file.ContentType);
                         }
                         
                         _logger.LogInformation($"[R2] Master upload complete.");
                         return r2Key;
                    }
                    catch (Exception r2Ex)
                    {
                        _logger.LogError(r2Ex, "[R2] Failed to upload master file");
                        return null; // non-fatal, but logged
                    }
                });

                // Define Task 2: Process Video (Thumbnail, Compress, Mux)
                var processingTask = Task.Run(async () => 
                {
                    // 1. Extract High-Res Thumbnail
                    string? r2ThumbKey = null;
                    var thumbName = $"{clipId}.jpg";
                    var thumbPath = Path.Combine(tempDir, thumbName);
                    
                    try 
                    {
                        _logger.LogInformation($"[Compression] Extracting thumbnail to {thumbPath}");
                        var thumbProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "ffmpeg",
                                Arguments = $"-ss 00:00:01 -i \"{inputPath}\" -vframes 1 -q:v 2 \"{thumbPath}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        thumbProcess.Start();
                        await thumbProcess.WaitForExitAsync();
                        
                        if (thumbProcess.ExitCode == 0)
                        {
                            // Upload thumbnail to R2
                            using var thumbStream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
                            r2ThumbKey = $"{evt.TenantId}/events/{evt.Id}/thumbnails/{thumbName}"; 
                            await _storageService.UploadAsync(thumbStream, r2ThumbKey, "image/jpeg");
                            
                            _logger.LogInformation($"[Compression] Thumbnail uploaded to R2: {r2ThumbKey}");
                            
                            // 2. AI Analysis (Best effort)
                            try
                            {
                                using var analysisStream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
                                var aiTags = await _visionService.AnalyzeImageAsync(analysisStream);
                                if (aiTags.Length > 0)
                                {
                                     // Store tags in a way we can retrieve? 
                                     // We can't use HttpContext inside Task.Run. Return them.
                                     return (r2ThumbKey, aiTags); 
                                }
                            }
                            catch (Exception) { /* AI failure ignored */ }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Compression] Thumbnail extraction exception");
                    }

                    return (r2ThumbKey, (string[])null);
                });

                // Task 3: Compress and Mux (Can run alongside Thumbnail? No, ffmpeg might contend for disk read? 
                // Actually, OS handles concurrent reads fine. Let's run Compress in parallel with Thumb too?
                // Mux requires the output of Compress.
                // So (R2 Master) || (Thumbnail) || (Compress -> Mux)
                
                var compressionAndMuxTask = Task.Run(async () => 
                {
                    // Compress with FFmpeg
                    var outputPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(file.FileName) + "_540p.mp4");
                    var stopwatch = Stopwatch.StartNew();

                    var ffmpegProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = $"-i \"{inputPath}\" -vf \"scale=-2:540\" -c:v libx264 -preset veryfast -crf 28 -c:a aac -b:a 128k \"{outputPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    ffmpegProcess.Start();
                    await ffmpegProcess.WaitForExitAsync();
                    stopwatch.Stop();
                    _logger.LogInformation($"[Compression] FFmpeg completed in {stopwatch.ElapsedMilliseconds / 1000.0:F1}s");

                    if (ffmpegProcess.ExitCode != 0)
                    {
                        throw new Exception("FFmpeg compression failed");
                    }

                    // Upload to Mux
                    var (muxUrl, uploadId) = await _videoService.CreateUploadUrlAsync(
                        clipId: clipId,
                        title: file.FileName,
                        creatorId: userId
                    );

                    using var httpClient = new HttpClient();
                    using var fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
                    using var content = new StreamContent(fileStream);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
                    
                    var uploadResponse = await httpClient.PutAsync(muxUrl, content);
                    if (!uploadResponse.IsSuccessStatusCode)
                    {
                         throw new Exception($"Mux upload failed: {uploadResponse.StatusCode}");
                    }
                    
                    return (uploadId, new FileInfo(outputPath).Length, stopwatch.ElapsedMilliseconds / 1000.0);
                });

                // Await all tasks
                await Task.WhenAll(r2UploadTask, processingTask, compressionAndMuxTask);
                
                var r2MasterKey = await r2UploadTask;
                var (r2ThumbKey, aiTags) = await processingTask;
                var (muxUploadId, compressedSize, compressionTime) = await compressionAndMuxTask;

                // Create Clip entity
                var clip = new Clip
                {
                    Id = clipId,
                    EventId = eventId,
                    TenantId = evt.TenantId,
                    Title = file.FileName,
                    PriceCents = priceCents,
                    MuxUploadId = muxUploadId,
                    MasterFileName = r2MasterKey, 
                    ThumbnailFileName = r2ThumbKey
                };

                if (aiTags != null && aiTags.Length > 0)
                {
                    clip.TagsJson = System.Text.Json.JsonSerializer.Serialize(aiTags);
                }

                if (DateTime.TryParse(lastModified, out var modifiedDate))
                {
                    clip.RecordingStartedAt = modifiedDate;
                }

                await _clipRepository.AddAsync(clip);
                _logger.LogInformation($"[Compression] Clip saved to database: {clipId}");

                // Start background polling
                _ = ResolveMuxDetailsAsync(clip);

                return Ok(new
                {
                    success = true,
                    clipId = clipId,
                    originalSize = file.Length,
                    compressedSize = compressedSize,
                    compressionTime = compressionTime
                });
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Compression] Failed to clean up temp dir: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Compression] Unexpected error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private Task ResolveMuxDetailsAsync(Clip clip)
    {
        // Offload to background thread to avoid blocking request and detaching from request context
        return Task.Run(async () => 
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClipCore.Infrastructure.Data.AppDbContext>();
            var videoService = scope.ServiceProvider.GetRequiredService<IVideoService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<VideoCompressionController>>();

            try 
            {
                // Re-fetch clip ignoring query filters to ensure we can update it even without Tenant Context
                var dbClip = await dbContext.Clips.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == clip.Id);
                if (dbClip == null) return;

                if (string.IsNullOrEmpty(dbClip.MuxUploadId)) return;

                logger.LogInformation($"[Compression] Starting background poll for AssetId: {dbClip.Title} (UploadId: {dbClip.MuxUploadId})");
                
                // 1. Poll for AssetId (up to 30 seconds)
                string? assetId = null;
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(2000);
                    assetId = await videoService.GetAssetIdFromUploadAsync(dbClip.MuxUploadId);
                    if (!string.IsNullOrEmpty(assetId)) break;
                }

                if (string.IsNullOrEmpty(assetId))
                {
                    logger.LogWarning($"[Compression] Timed out waiting for AssetId for {dbClip.Title}");
                    return;
                }

                logger.LogInformation($"[Compression] Resolved AssetId: {assetId} for {dbClip.Title}");
                dbClip.MuxAssetId = assetId;
                await dbContext.SaveChangesAsync(); // Save AssetID immediately

                // 2. Poll for Duration and PlaybackId (up to 60 seconds)
                for (int i = 0; i < 20; i++)
                {
                    var (duration, startedAt) = await videoService.GetAssetDetailsAsync(assetId);
                    logger.LogInformation($"[Compression] Polling duration for {dbClip.Title}: {(duration.HasValue ? duration.Value.ToString() : "null")}");
                    
                    if (duration.HasValue && duration.Value > 0)
                    {
                        dbClip.DurationSec = duration;
                        
                        if (startedAt.HasValue) 
                        {
                            dbClip.RecordingStartedAt = startedAt;
                            logger.LogInformation($"[Compression] Upgraded timestamp to Mux Metadata: {startedAt}");
                        }
                        
                        // Ensure we have a Playback ID for the thumbnail
                        var playbackId = await videoService.EnsurePlaybackIdAsync(assetId);
                        if (!string.IsNullOrEmpty(playbackId))
                        {
                            dbClip.PlaybackIdSigned = playbackId;
                        }

                        await dbContext.SaveChangesAsync();
                        logger.LogInformation($"[Compression] SUCCESS: Resolved playback details for {dbClip.Title}");
                        return;
                    }

                    logger.LogInformation($"[Compression] Still waiting for duration for {dbClip.Title} (attempt {i+1}/20)...");
                    await Task.Delay(3000); // Wait 3s between attempts
                }

                // Final save even if duration is still missing (fallback)
                await dbContext.SaveChangesAsync();
                logger.LogWarning($"[Compression] Metadata polling finished for {dbClip.Title} (Duration might still be processing)");
            }
            catch (Exception ex)
            {
                 logger.LogError(ex, $"[Compression] Error in background polling for {clip.Id}");
            }
        });
    }
}
