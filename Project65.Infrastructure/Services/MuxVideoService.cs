using Microsoft.Extensions.Configuration;
using Mux.Csharp.Sdk.Api;
using Mux.Csharp.Sdk.Client;
using Mux.Csharp.Sdk.Model;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Services;

public class MuxVideoService : IVideoService
{
    private readonly string _tokenId;
    private readonly string _tokenSecret;
    private readonly DirectUploadsApi _directUploadsApi;
    private readonly AssetsApi _assetsApi;
    private readonly string _signingKeyId;
    private readonly string _signingKeyPrivate;

    public MuxVideoService(IConfiguration configuration)
    {
        _tokenId = configuration["Mux:TokenId"] ?? throw new ArgumentNullException("Mux:TokenId");
        _tokenSecret = configuration["Mux:TokenSecret"] ?? throw new ArgumentNullException("Mux:TokenSecret");
        _signingKeyId = configuration["Mux:SigningKeyId"] ?? ""; 
        _signingKeyPrivate = configuration["Mux:SigningKeyPrivate"] ?? "";

        var config = new Configuration();
        config.BasePath = "https://api.mux.com";
        config.Username = _tokenId;
        config.Password = _tokenSecret;

        _directUploadsApi = new DirectUploadsApi(config);
        _assetsApi = new AssetsApi(config);
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
        request.CorsOrigin = "*";
        
        var result = await _directUploadsApi.CreateDirectUploadAsync(request);
        return (result.Data.Url, result.Data.Id);
    }

    public async Task<string?> GetPlaybackIdAsync(string assetId)
    {
        var asset = await _assetsApi.GetAssetAsync(assetId);
        return asset.Data.PlaybackIds?.FirstOrDefault()?.Id;
    }

    public async Task<string?> EnsurePlaybackIdAsync(string assetId)
    {
        var asset = await _assetsApi.GetAssetAsync(assetId);
        var existingId = asset.Data.PlaybackIds?.FirstOrDefault()?.Id;

        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        // No playback ID exists, create one with Signed policy
        var req = new CreatePlaybackIDRequest(policy: PlaybackPolicy.Signed);
        var newPlaybackId = await _assetsApi.CreateAssetPlaybackIdAsync(assetId, req);
        
        return newPlaybackId.Data.Id;
    }

    public string GetPlaybackToken(string playbackId, string audience = "v", string? maxResolution = null)
    {
        if (string.IsNullOrEmpty(_signingKeyId) || string.IsNullOrEmpty(_signingKeyPrivate))
        {
            return "";
        }
        return GenerateSignedToken(playbackId, audience, maxResolution);
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
            var exp = unixNow + (3600 * 2); // 2 hours

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
            Console.WriteLine($"[MUX-ERROR] Token Generation Failed: {ex.Message}");
            return "";
        }
    }

    public async Task DeleteAssetAsync(string assetId)
    {
        if (string.IsNullOrEmpty(assetId)) return;
        try 
        {
            await _assetsApi.DeleteAssetAsync(assetId);
        }
        catch (Exception ex)
        {
            // Log or handle error (e.g. if already deleted)
            Console.WriteLine($"Error deleting Mux asset {assetId}: {ex.Message}");
        }
    }

    public async Task<string?> GetAssetIdFromUploadAsync(string uploadId)
    {
        if (string.IsNullOrEmpty(uploadId)) return null;
        try 
        {
            var upload = await _directUploadsApi.GetDirectUploadAsync(uploadId);
            return upload.Data.AssetId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Mux upload {uploadId}: {ex.Message}");
            return null;
        }
    }

    public async Task<double?> GetAssetDurationAsync(string assetId)
    {
        if (string.IsNullOrEmpty(assetId)) return null;
        try 
        {
            var result = await _assetsApi.GetAssetAsync(assetId);
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
            var result = await _assetsApi.GetAssetAsync(assetId);
            
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
            Console.WriteLine($"Error fetching Mux asset details {assetId}: {ex.Message}");
            return (null, null);
        }
    }
    public async Task<string?> GetDownloadUrlAsync(string assetId, string? fileName = null)
    {
        if (string.IsNullOrEmpty(assetId)) return null;

        try
        {
            var asset = await _assetsApi.GetAssetAsync(assetId);
            
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
                Console.WriteLine($"[MUX-DEBUG] Asset {assetId} master is preparing.");
                return null; // Not ready yet
            }

            // If we are here, Master Access is likely "none" or "errored" (though error usually throws).
            // Request Master Access
            if (asset.Data.MasterAccess == Asset.MasterAccessEnum.None)
            {
                 Console.WriteLine($"[MUX-DEBUG] Enabling Master Access for {assetId}");
                 var updateRequest = new UpdateAssetMasterAccessRequest(masterAccess: UpdateAssetMasterAccessRequest.MasterAccessEnum.Temporary);
                 await _assetsApi.UpdateAssetMasterAccessAsync(assetId, updateRequest);
            }

            return null; // Request sent, user needs to wait.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MUX-ERROR] Error resolving Master URL for {assetId}: {ex.Message}");
            return null;
        }
    }
}
