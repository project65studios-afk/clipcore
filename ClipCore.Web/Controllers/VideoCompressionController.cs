using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClipCore.Core.Interfaces;
using ClipCore.Core.Entities;
using System.Diagnostics;

namespace ClipCore.Web.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("admin")]
public class VideoCompressionController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly IClipRepository _clipRepository;
    private readonly IStorageService _storageService;
    private readonly IVisionService _visionService;
    private readonly ILogger<VideoCompressionController> _logger;

    public VideoCompressionController(
        IVideoService videoService,
        IClipRepository clipRepository,
        IStorageService storageService,
        IVisionService visionService,
        ILogger<VideoCompressionController> logger)
    {
        _videoService = videoService;
        _clipRepository = clipRepository;
        _storageService = storageService;
        _visionService = visionService;
        _logger = logger;
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

            var tempDir = Path.Combine(Path.GetTempPath(), "video-compression", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Save uploaded file
                var inputPath = Path.Combine(tempDir, file.FileName);
                using (var stream = new FileStream(inputPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"[Compression] Received {file.FileName}, size: {file.Length / 1024 / 1024}MB");

                var clipId = Guid.NewGuid().ToString();

                // 1. Extract High-Res Thumbnail (from original input)
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
                            // Extract frame at 1s, qscale:v 2 for high quality JPG
                            Arguments = $"-ss 00:00:01 -i \"{inputPath}\" -vframes 1 -q:v 2 \"{thumbPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    thumbProcess.Start();
                    await thumbProcess.WaitForExitAsync();
                    
                    if (thumbProcess.ExitCode != 0)
                    {
                         var err = await thumbProcess.StandardError.ReadToEndAsync();
                         _logger.LogWarning($"[Compression] Thumbnail extraction failed: {err}");
                         thumbName = null; // Fallback to Mux
                    }
                    else
                    {
                        // Upload thumbnail to R2
                        using var thumbStream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
                        var r2ThumbKey = $"thumbnails/{thumbName}"; // Standardized path
                        await _storageService.UploadAsync(thumbStream, r2ThumbKey, "image/jpeg");
                        
                        _logger.LogInformation($"[Compression] Thumbnail uploaded to R2: {r2ThumbKey}");
                        
                        // Close stream so we can delete file
                        thumbStream.Close();
                        
                        // 2. AI Analysis (Async - don't block upload too long, but wait since it's fast)
                        // Trigger AI Analysis on the local high-res thumbnail
                        try
                        {
                            _logger.LogInformation($"[AI] Analyzing frame for auto-tagging...");
                            using var analysisStream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
                            var aiTags = await _visionService.AnalyzeImageAsync(analysisStream);
                            if (aiTags.Length > 0)
                            {
                                 _logger.LogInformation($"[AI] Identified tags: {string.Join(", ", aiTags)}");
                                // We'll save these to the Clip object later
                                HttpContext.Items["AiTags"] = aiTags; 
                            }
                        }
                        catch (Exception aiEx)
                        {
                            _logger.LogError($"[AI] Analysis failed: {aiEx.Message}");
                        }
                        
                        // Delete local thumbnail
                        System.IO.File.Delete(thumbPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Compression] Thumbnail extraction exception");
                    thumbName = null;
                }

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
                
                // Read Output/Error streams asynchronously to avoid deadlock
                var stdoutTask = ffmpegProcess.StandardOutput.ReadToEndAsync();
                var stderrTask = ffmpegProcess.StandardError.ReadToEndAsync();
                var ffmpegExitTask = ffmpegProcess.WaitForExitAsync();

                // Wait for compression to complete
                await Task.WhenAll(ffmpegExitTask, stdoutTask, stderrTask);

                stopwatch.Stop();
                _logger.LogInformation($"[Compression] FFmpeg completed in {stopwatch.ElapsedMilliseconds / 1000.0:F1}s");

                if (ffmpegProcess.ExitCode != 0)
                {
                    var error = await stderrTask; // we already awaited it
                    _logger.LogError($"[Compression] FFmpeg error: {error}");
                    return StatusCode(500, new { error = "Video compression failed" });
                }

                // Get file info
                var compressedFile = new FileInfo(outputPath);
                _logger.LogInformation($"[Compression] Output size: {compressedFile.Length / 1024 / 1024}MB");

                // Upload to Mux
                // clipId is already generated above
                var (muxUrl, uploadId) = await _videoService.CreateUploadUrlAsync(
                    clipId: clipId,
                    title: file.FileName,
                    creatorId: userId
                );

                // Upload compressed file to Mux using HttpClient
                using var httpClient = new HttpClient();
                using var fileStream = System.IO.File.OpenRead(outputPath);
                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
                
                var uploadResponse = await httpClient.PutAsync(muxUrl, content);
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"[Compression] Mux upload failed: {uploadResponse.StatusCode}");
                    return StatusCode(500, new { error = "Mux upload failed" });
                }

                _logger.LogInformation($"[Compression] Uploaded to Mux successfully");

                // Create Clip entity
                var clip = new Clip
                {
                    Id = clipId,
                    EventId = eventId,
                    Title = file.FileName,
                    PriceCents = priceCents,
                    MuxUploadId = uploadId,
                    MasterFileName = null, // No R2 master for standard event uploads
                    ThumbnailFileName = $"thumbnails/{thumbName}"
                };

                // Apply AI Tags if found
                if (HttpContext.Items.TryGetValue("AiTags", out var tagsObj) && tagsObj is string[] tags)
                {
                    clip.TagsJson = System.Text.Json.JsonSerializer.Serialize(tags);
                }

                // Capture browser's Modified Date if provided
                if (DateTime.TryParse(lastModified, out var modifiedDate))
                {
                    clip.RecordingStartedAt = modifiedDate;
                    _logger.LogInformation($"[Compression] Saved LastModified date: {modifiedDate}");
                }

                await _clipRepository.AddAsync(clip);
                _logger.LogInformation($"[Compression] Clip saved to database: {clipId}");

                // Start background polling to resolve Asset ID and Playback ID
                _ = ResolveMuxDetailsAsync(clip);

                return Ok(new
                {
                    success = true,
                    clipId = clipId,
                    originalSize = file.Length,
                    compressedSize = compressedFile.Length,
                    compressionTime = stopwatch.ElapsedMilliseconds / 1000.0
                });
            }
            finally
            {
                // Clean up temp files
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

    private async Task ResolveMuxDetailsAsync(Clip clip)
    {
        if (string.IsNullOrEmpty(clip.MuxUploadId)) return;

        _logger.LogInformation($"[Compression] Starting background poll for AssetId: {clip.Title} (UploadId: {clip.MuxUploadId})");
        
        // 1. Poll for AssetId (up to 30 seconds)
        string? assetId = null;
        for (int i = 0; i < 15; i++)
        {
            await Task.Delay(2000);
            assetId = await _videoService.GetAssetIdFromUploadAsync(clip.MuxUploadId);
            if (!string.IsNullOrEmpty(assetId)) break;
        }

        if (string.IsNullOrEmpty(assetId))
        {
            _logger.LogWarning($"[Compression] Timed out waiting for AssetId for {clip.Title}");
            return;
        }

        _logger.LogInformation($"[Compression] Resolved AssetId: {assetId} for {clip.Title}");
        clip.MuxAssetId = assetId;

        // 2. Poll for Duration and PlaybackId (up to 60 seconds)
        for (int i = 0; i < 20; i++)
        {
            var (duration, startedAt) = await _videoService.GetAssetDetailsAsync(assetId);
            _logger.LogInformation($"[Compression] Polling duration for {clip.Title}: {(duration.HasValue ? duration.Value.ToString() : "null")}");
            
            if (duration.HasValue && duration.Value > 0)
            {
                clip.DurationSec = duration;
                
                // Mux Metadata Date > Browser Modified Date
                // Since MuxVideoService.cs no longer falls back to Upload Time, 'startedAt' is a REAL creation date from metadata/atoms.
                // We trust this more than the user's file system modified date, so we overwrite.
                if (startedAt.HasValue) 
                {
                    clip.RecordingStartedAt = startedAt;
                    _logger.LogInformation($"[Compression] Upgraded timestamp to Mux Metadata: {startedAt}");
                }
                
                // Ensure we have a Playback ID for the thumbnail
                var playbackId = await _videoService.EnsurePlaybackIdAsync(assetId);
                if (!string.IsNullOrEmpty(playbackId))
                {
                    clip.PlaybackIdSigned = playbackId;
                }

                await _clipRepository.UpdateAsync(clip);
                _logger.LogInformation($"[Compression] SUCCESS: Resolved playback details for {clip.Title}");
                return;
            }

            _logger.LogInformation($"[Compression] Still waiting for duration for {clip.Title} (attempt {i+1}/20)...");
            await Task.Delay(3000); // Wait 3s between attempts
        }

        // Final save even if duration is still missing (fallback)
        await _clipRepository.UpdateAsync(clip);
        _logger.LogWarning($"[Compression] Metadata polling finished for {clip.Title} (Duration might still be processing)");
    }
}
