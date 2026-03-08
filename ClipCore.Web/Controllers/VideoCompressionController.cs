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
        [FromForm] string? collectionId,
        [FromForm] string? masterFileName,
        [FromForm] int priceCents,
        [FromForm] int priceCommercialCents,
        [FromForm] string? userId,
        [FromForm] string? lastModified = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (string.IsNullOrEmpty(collectionId))
                return BadRequest(new { error = "Collection ID is required" });

            var tempDir = Path.Combine(Path.GetTempPath(), "video-compression", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Sanitize input: Use a secure extension-only filename to prevent path traversal/command injection
                var inputExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(inputExtension)) inputExtension = ".mp4";
                var inputPath = Path.Combine(tempDir, "input" + inputExtension);

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
                    CollectionId = collectionId,
                    Title = file.FileName,
                    PriceCents = priceCents,
                    PriceCommercialCents = priceCommercialCents,
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
                    clip.RecordingStartedAt = modifiedDate.ToUniversalTime();
                    _logger.LogInformation($"[Compression] Saved LastModified date: {clip.RecordingStartedAt}");
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

    [HttpPost("batch-delete-errored")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BatchDeleteErrored()
    {
        try
        {
            var count = await _videoService.DeleteErroredAssetsAsync();
            return Ok(new { message = $"Successfully deleted {count} errored assets from Mux." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch delete errored assets");
            return StatusCode(500, new { error = "Internal server error during Mux cleanup" });
        }
    }

    [HttpPost("get-direct-upload-url")]
    public async Task<IActionResult> GetDirectUploadUrl([FromBody] DirectUploadUrlRequest request)
    {
        try
        {
            var clipId = Guid.NewGuid().ToString();
            var (url, uploadId) = await _videoService.CreateDirectUploadUrlAsync(request.Title, request.UserId, clipId);
            _logger.LogInformation($"[DirectUpload] Generated URL for '{request.Title}': '{url}' (ID: {uploadId}, ClipID: {clipId})");

            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("[DirectUpload] Mux returned empty URL!");
                return StatusCode(500, new { error = "Mux returned empty URL" });
            }
            return Ok(new { url, uploadId, clipId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create direct upload URL");
            return StatusCode(500, new { error = "Could not create upload URL" });
        }
    }

    public class DirectUploadUrlRequest
    {
        public string? Title { get; set; }
        public string? UserId { get; set; }
    }

    [HttpPost("confirm-direct-upload")]
    public async Task<IActionResult> ConfirmDirectUpload([FromBody] DirectUploadConfirmRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.MuxUploadId) || string.IsNullOrEmpty(request.CollectionId))
                return BadRequest(new { error = "Missing required fields" });

            // 1. Create Clip Entity
            var clipId = request.ClipId ?? Guid.NewGuid().ToString();
            var clip = new Clip
            {
                Id = clipId,
                CollectionId = request.CollectionId,
                Title = request.Title ?? "Untitled",
                PriceCents = request.PriceCents,
                PriceCommercialCents = request.PriceCommercialCents,
                MuxUploadId = request.MuxUploadId,
                IsDirectUpload = true,
                ThumbnailFileName = request.ThumbnailKeys.FirstOrDefault(), // Primary thumb
                PublishedAt = DateTime.UtcNow,
                AllowGifSale = request.AllowGifSale,
                GifPriceCents = request.GifPriceCents
            };

            // 2. Parse creation date
            if (DateTime.TryParse(request.LastModified, out var modifiedDate))
            {
                clip.RecordingStartedAt = modifiedDate.ToUniversalTime();
            }

            // 3. AI Analysis (Parallel)
            if (request.ThumbnailKeys != null && request.ThumbnailKeys.Any())
            {
                var allTags = new List<string>();
                using var http = new HttpClient();

                // Analyze up to 3 thumbnails
                var tasks = request.ThumbnailKeys.Take(3).Select(async key =>
                {
                    try
                    {
                        // Get secure URL to read the file from R2
                        var url = _storageService.GetPresignedDownloadUrl(key);
                        using var stream = await http.GetStreamAsync(url);
                        var tags = await _visionService.AnalyzeImageAsync(stream);
                        lock (allTags)
                        {
                            allTags.AddRange(tags);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[AI] Failed to analyze direct thumb {key}");
                    }
                });

                await Task.WhenAll(tasks);

                if (allTags.Any())
                {
                    // De-duplicate and save
                    var uniqueTags = allTags.Distinct().ToArray();
                    clip.TagsJson = System.Text.Json.JsonSerializer.Serialize(uniqueTags);
                    _logger.LogInformation($"[AI] Direct Analysis found tags: {string.Join(", ", uniqueTags)}");
                }
            }

            // 4. Save to DB
            await _clipRepository.AddAsync(clip);
            _logger.LogInformation($"[DirectUpload] Clip saved: {clipId}");

            // 5. Start Background Polling
            _ = ResolveMuxDetailsAsync(clip);

            return Ok(new { success = true, clipId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm direct upload");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class DirectUploadConfirmRequest
    {
        public string MuxUploadId { get; set; } = string.Empty;
        public string? ClipId { get; set; }
        public string CollectionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int PriceCents { get; set; }
        public int PriceCommercialCents { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? LastModified { get; set; }
        public List<string> ThumbnailKeys { get; set; } = new();
        public bool AllowGifSale { get; set; }
        public int GifPriceCents { get; set; }
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

            _logger.LogInformation($"[Compression] Still waiting for duration for {clip.Title} (attempt {i + 1}/20)...");
            await Task.Delay(3000); // Wait 3s between attempts
        }

        // Final save even if duration is still missing (fallback)
        await _clipRepository.UpdateAsync(clip);
        _logger.LogWarning($"[Compression] Metadata polling finished for {clip.Title} (Duration might still be processing)");
    }
}
