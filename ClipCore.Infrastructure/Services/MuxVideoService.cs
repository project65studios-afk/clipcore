using Microsoft.Extensions.Configuration;
using Mux.Csharp.Sdk.Api;
using Mux.Csharp.Sdk.Client;
using Mux.Csharp.Sdk.Model;
using ClipCore.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using ClipCore.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ClipCore.Infrastructure.Services;

public class MuxVideoService : IVideoService
{
    private readonly IConfiguration _configuration;
    private readonly string _tokenId;
    private readonly string _tokenSecret;
    private readonly DirectUploadsApi _directUploadsApi;
    private readonly AssetsApi _assetsApi;
    private readonly string _signingKeyId;
    private readonly string _signingKeyPrivate;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUsageRepository _usageRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MuxVideoService> _logger;
    private const int MaxDailyTokens = 200; // Lowered to 200 (human-safe, bot-hostile)

    private readonly Polly.ResiliencePipeline _resiliencePipeline;
    

    public MuxVideoService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IUsageRepository usageRepository, IMemoryCache cache, ILogger<MuxVideoService> logger)
    {
        _configuration = configuration;
        _tokenId = configuration["Mux:TokenId"] ?? throw new ArgumentNullException("Mux:TokenId");
        _tokenSecret = configuration["Mux:TokenSecret"] ?? throw new ArgumentNullException("Mux:TokenSecret");
        _signingKeyId = configuration["Mux:SigningKeyId"] ?? ""; 
        _signingKeyPrivate = configuration["Mux:SigningKeyPrivate"] ?? "";
        _httpContextAccessor = httpContextAccessor;
        _usageRepository = usageRepository;
        _cache = cache;
        _logger = logger;

        var config = new Configuration();
        config.BasePath = "https://api.mux.com";
        config.Username = _tokenId;
        config.Password = _tokenSecret;

        _directUploadsApi = new DirectUploadsApi(config);
        _assetsApi = new AssetsApi(config);
        
        // Define Polly Resilience Pipeline (Retry w/ Exponential Backoff)
        _resiliencePipeline = new Polly.ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new Polly.PredicateBuilder().Handle<ApiException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = Polly.DelayBackoffType.Exponential,
                OnRetry = static args =>
                {
                    Console.WriteLine($"[MUX-RETRY] Retrying Mux API call... (Attempt {args.AttemptNumber})");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private string GetAppOrigin()
    {
        var origins = _configuration.GetSection("AllowedOrigins").Get<string[]>();
        return origins?.FirstOrDefault() ?? "http://localhost:5094";
    }

    public async Task<(string url, string uploadId)> CreateUploadUrlAsync(string clipId, string title, string? creatorId = null)
    {
        // Set metadata for Mux Data and Dashboard visibility
        var metadata = new AssetMetadata 
        {
            Title = title,
            CreatorId = creatorId ?? "Admin",
            ExternalId = clipId
        };
        
        var assetSettings = new CreateAssetRequest(
            playbackPolicy: new List<PlaybackPolicy> { PlaybackPolicy.Signed },
            passthrough: clipId, // Link to our local database ID
            meta: metadata
        );
        
        var request = new CreateUploadRequest(newAssetSettings: assetSettings);
        request.CorsOrigin = GetAppOrigin();
        
        var result = await _resiliencePipeline.ExecuteAsync(async ct => await _directUploadsApi.CreateDirectUploadAsync(request, cancellationToken: ct));
        return (result.Data.Url, result.Data.Id);
    }

    public async Task<(string url, string uploadId)> CreateFulfillmentUploadUrlAsync(int purchaseId)
    {
        // Passthrough belongs on CreateAssetRequest, not AssetMetadata in some SDK versions, 
        // OR it's a separate parameter. 
        // Let's try setting it on CreateAssetRequest if possible, or check how the SDK expects it.
        // Actually, looking at typical Mux SDK, Passthrough is often a property of CreateAssetRequest.
        
        // Correction: Remove Passthrough from AssetMetadata
        var metadata = new AssetMetadata 
        {
            // Title might not be here either depending on SDK version? 
            // Usually metadata allows custom fields but Title/Passthrough are top level in API.
            // But Mux.C# SDK (official) maps them. 
            // If AssetMetadata failed, let's remove it from there.
        };
        
        var assetSettings = new CreateAssetRequest(
            playbackPolicy: new List<PlaybackPolicy> { PlaybackPolicy.Signed },
            masterAccess: CreateAssetRequest.MasterAccessEnum.Temporary,
            passthrough: $"fulfillment:{purchaseId}" // Add it here if constructor supports it
        );
        
        // If constructor doesn't support it, we might need to set property:
        // assetSettings.Passthrough = ...
        
        // Let we try to instantiate nicely.
        
        return await CreateFulfillmentUploadUrlAsync_Fixed(purchaseId);
    }

    private async Task<(string url, string uploadId)> CreateFulfillmentUploadUrlAsync_Fixed(int purchaseId)
    {
         var assetSettings = new CreateAssetRequest(
            playbackPolicy: new List<PlaybackPolicy> { PlaybackPolicy.Signed },
            masterAccess: CreateAssetRequest.MasterAccessEnum.Temporary
        );
        assetSettings.Passthrough = $"fulfillment:{purchaseId}";
        
        // Is 'Title' supported? In standard Mux API, title is often just a metadata field (arbitrary) or not standard.
        // Let's remove Title from code to be safe if AssetMetadata fails, or assume it's custom.
        // Mux often puts everything in 'test' map if not standard.
        
        var request = new CreateUploadRequest(newAssetSettings: assetSettings);
        request.CorsOrigin = GetAppOrigin();
        
        var result = await _directUploadsApi.CreateDirectUploadAsync(request);
        return (result.Data.Url, result.Data.Id);
    }

    public async Task<string?> GetPlaybackIdAsync(string assetId)
    {
        var asset = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.GetAssetAsync(assetId, cancellationToken: ct));
        return asset.Data.PlaybackIds?.FirstOrDefault()?.Id;
    }

    public async Task<string?> EnsurePlaybackIdAsync(string assetId)
    {
        var asset = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.GetAssetAsync(assetId, cancellationToken: ct));
        var existingId = asset.Data.PlaybackIds?.FirstOrDefault()?.Id;

        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        // No playback ID exists, create one with Signed policy
        var req = new CreatePlaybackIDRequest(policy: PlaybackPolicy.Signed);
        var newPlaybackId = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.CreateAssetPlaybackIdAsync(assetId, req, cancellationToken: ct));
        
        return newPlaybackId.Data.Id;
    }

    public async Task<string> GetPlaybackToken(string playbackId, string audience = "v", string? maxResolution = null)
    {
        try
        {
            long unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var context = _httpContextAccessor.HttpContext;
            string ip = context?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            if (ip == "::1") ip = "127.0.0.1";

            // ANTI-BOT: Block known non-browser user agents
            try 
            {
                string ua = context?.Request?.Headers["User-Agent"].ToString() ?? "";
                if (ua.Contains("curl", StringComparison.OrdinalIgnoreCase) || 
                    ua.Contains("python", StringComparison.OrdinalIgnoreCase) || 
                    ua.Contains("wget", StringComparison.OrdinalIgnoreCase) ||
                    ua.Contains("postman", StringComparison.OrdinalIgnoreCase))
                {
                     _logger.LogWarning($"[BOT BLOCKED] UA: {ua} IP: {ip}");
                     return ""; // Block
                }
            }
            catch {} // Ignore UA check failures (e.g. if Request is null)

            // CACHE CHECK
            string cacheKey = $"mux_token_{ip}_{playbackId}_{audience}_{maxResolution ?? "full"}";
            
            if (_cache.TryGetValue(cacheKey, out string? cachedToken))
            {
                if (!string.IsNullOrEmpty(cachedToken)) return cachedToken;
            }

            if (string.IsNullOrEmpty(_signingKeyId) || string.IsNullOrEmpty(_signingKeyPrivate))
            {
                return "";
            }

            // Only enforce limits for video playback ('v'), not thumbnails ('t', 's')
            if (audience == "v")
            {
                var date = DateOnly.FromDateTime(DateTime.UtcNow);
                
                // Allow Admins AND low-res previews (480p) to bypass limits
                bool isAdmin = context?.User?.IsInRole("Admin") ?? false;
                bool isPreview = !string.IsNullOrEmpty(maxResolution); // Previews send "540p"
                
                if (!isAdmin && !isPreview && ip != "unknown")
                {
                    // Tiered Limits: 400 for Logged In, 200 for Anonymous
                    bool isAuthenticated = context?.User?.Identity?.IsAuthenticated == true;
                    int dailyLimit = isAuthenticated ? 400 : 200;

                    var usage = await _usageRepository.GetUsageAsync(ip, date);
                    if (usage.TokenRequestCount >= dailyLimit)
                    {
                        _logger.LogWarning($"[LIMIT] IP {ip} exceeded daily limit ({usage.TokenRequestCount}/{dailyLimit}). Denying token.");
                        return ""; 
                    }

                    await _usageRepository.IncrementUsageAsync(ip, date);
                }
            }

            var token = GenerateSignedToken(playbackId, audience, maxResolution);
            
            // CACHE SET (10 mins)
            if (!string.IsNullOrEmpty(token))
            {
                _cache.Set(cacheKey, token, TimeSpan.FromMinutes(10));
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[MUX-CRITICAL-ERROR] GetPlaybackToken Failed");
            return ""; // Fail safe
        }
    }

    private string GenerateSignedToken(string playbackId, string audience, string? maxResolution = null)
    {
        try 
        {
            byte[] keyBytes = Convert.FromBase64String(_signingKeyPrivate);
            var rsa = System.Security.Cryptography.RSA.Create();
            
            string pem = System.Text.Encoding.UTF8.GetString(keyBytes);
            if (pem.Contains("PRIVATE KEY"))
            {
                var base64 = pem
                    .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                    .Replace("-----END RSA PRIVATE KEY-----", "")
                    .Replace("-----BEGIN PRIVATE KEY-----", "")
                    .Replace("-----END PRIVATE KEY-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Trim();
                var derBytes = Convert.FromBase64String(base64);
                if (pem.Contains("RSA PRIVATE KEY")) rsa.ImportRSAPrivateKey(derBytes, out _);
                else rsa.ImportPkcs8PrivateKey(derBytes, out _);
            }
            else 
            {
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            }

            var securityKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa) { KeyId = _signingKeyId };
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

            var now = DateTime.UtcNow;
            var unixNow = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            // Shorten expiry to 15 minutes (900 seconds) to force frequent renewal and usage tracking
            var exp = unixNow + 900; 

            // Manual Header/Payload to avoid extra claims like 'iat' or 'nbf' if Mux is picky
            var header = new System.IdentityModel.Tokens.Jwt.JwtHeader(credentials);
            header["typ"] = "JWT";
            if (!header.ContainsKey("kid")) header["kid"] = _signingKeyId;

            var payload = new System.IdentityModel.Tokens.Jwt.JwtPayload
            {
                { "sub", playbackId },
                { "aud", audience },
                { "exp", exp },
                { "iat", unixNow },
                { "nbf", unixNow - 30 }, // 30 seconds ago to handle clock skew
                { "kid", _signingKeyId }
            };

            if (!string.IsNullOrEmpty(maxResolution))
            {
                payload["max_resolution"] = maxResolution;
            }

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(header, payload);
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(token);

            Console.WriteLine($"[MUX-DEBUG] {audience} token for {playbackId}. KID:{header["kid"]} RES:{maxResolution ?? "Full"}");
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MUX-ERROR] Token Generation Failed");
            return "";
        }
    }

    public async Task DeleteAssetAsync(string assetId)
    {
        if (string.IsNullOrEmpty(assetId)) return;
        try 
        {
            await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.DeleteAssetAsync(assetId, cancellationToken: ct));
        }
        catch (Exception ex)
        {
            // Log or handle error (e.g. if already deleted)
            _logger.LogError(ex, $"Error deleting Mux asset {assetId}");
        }
    }

    public async Task<string?> GetAssetIdFromUploadAsync(string uploadId)
    {
        if (string.IsNullOrEmpty(uploadId)) return null;
        try 
        {
            var upload = await _resiliencePipeline.ExecuteAsync(async ct => await _directUploadsApi.GetDirectUploadAsync(uploadId, cancellationToken: ct));
            return upload.Data.AssetId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Mux upload {uploadId}");
            return null;
        }
    }

    public async Task<double?> GetAssetDurationAsync(string assetId)
    {
        if (string.IsNullOrEmpty(assetId)) return null;
        try 
        {
            var result = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.GetAssetAsync(assetId, cancellationToken: ct));
            return result.Data.Duration;
        }
        catch 
        {
            return null;
        }
    }

    public async Task<(double? duration, DateTime? startedAt)> GetAssetDetailsAsync(string assetId)
    {
        if (string.IsNullOrEmpty(assetId)) return (null, null);
        try 
        {
            var result = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.GetAssetAsync(assetId, cancellationToken: ct));
            
            // Only capture duration if the asset is fully ready
            if (result.Data.Status != Asset.StatusEnum.Ready)
            {
                return (null, null);
            }

            var duration = result.Data.Duration;
            
            DateTime? startedAt = null;
            if (result.Data.RecordingTimes?.Any() == true)
            {
                // RecordingTimes is a list of segments, usually the first one has the start time
                var firstRecord = result.Data.RecordingTimes.OrderBy(r => r.StartedAt).FirstOrDefault();
                if (firstRecord != null && firstRecord.StartedAt != default)
                {
                    startedAt = firstRecord.StartedAt;
                }
            }
            
            return (duration, startedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Mux asset details {assetId}");
            return (null, null);
        }
    }
    public async Task<string?> GetDownloadUrlAsync(string assetId, string? fileName = null)
    {
        if (string.IsNullOrEmpty(assetId)) return null;

        try
        {
            var asset = await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.GetAssetAsync(assetId, cancellationToken: ct));
            
            // Check Master Access Status
            var masterStatus = asset.Data.Master?.Status;
            var masterUrl = asset.Data.Master?.Url;

            Console.WriteLine($"[MUX-DEBUG] Asset {assetId} Master Status: {masterStatus}");

            if (masterStatus == AssetMaster.StatusEnum.Ready && !string.IsNullOrEmpty(masterUrl))
            {
                 // Master is ready.
                 // If a filename is provided, append it to the URL.
                 if (!string.IsNullOrEmpty(fileName))
                 {
                     // Ensure valid filename characters if needed, but Mux just needs it encoded.
                     // Append &download=filename (or ? if it was the first param, but signed URLs have params)
                     var separator = masterUrl.Contains("?") ? "&" : "?";
                     var encodedName = Uri.EscapeDataString(fileName);
                     return $"{masterUrl}{separator}download={encodedName}";
                 }
                 return masterUrl;
            }

            if (masterStatus == AssetMaster.StatusEnum.Preparing)
            {
                return null; // Not ready yet
            }

            // If we are here, Master Access is likely "none" or "errored" (though error usually throws).
            // Request Master Access
            if (asset.Data.MasterAccess == Asset.MasterAccessEnum.None)
            {
                 var updateRequest = new UpdateAssetMasterAccessRequest(masterAccess: UpdateAssetMasterAccessRequest.MasterAccessEnum.Temporary);
                 await _resiliencePipeline.ExecuteAsync(async ct => await _assetsApi.UpdateAssetMasterAccessAsync(assetId, updateRequest, cancellationToken: ct));
            }

            return null; // Request sent, user needs to wait.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MUX-ERROR] Error resolving Master URL for {assetId}");
            return null;
        }
    }
}
