using ClipCore.API.Interfaces;
using Mux.Csharp.Sdk.Api;
using Mux.Csharp.Sdk.Client;
using Mux.Csharp.Sdk.Model;

namespace ClipCore.API.Services;

public class MuxService : IMuxService
{
    private readonly DirectUploadsApi _directUploadsApi;
    private readonly AssetsApi _assetsApi;
    private readonly string _corsOrigin;
    private readonly ILogger<MuxService> _logger;

    public MuxService(IConfiguration config, ILogger<MuxService> logger)
    {
        _logger = logger;

        var tokenId     = config["Mux:TokenId"]     ?? throw new InvalidOperationException("Mux:TokenId not configured");
        var tokenSecret = config["Mux:TokenSecret"] ?? throw new InvalidOperationException("Mux:TokenSecret not configured");

        var muxConfig = new Configuration();
        muxConfig.BasePath = "https://api.mux.com";
        muxConfig.Username = tokenId;
        muxConfig.Password = tokenSecret;

        _directUploadsApi = new DirectUploadsApi(muxConfig);
        _assetsApi        = new AssetsApi(muxConfig);

        var origins = config.GetSection("AllowedOrigins").Get<string[]>();
        _corsOrigin = origins?.FirstOrDefault() ?? "http://localhost:3000";
    }

    public async Task<(string uploadUrl, string uploadId)> CreateDirectUploadAsync()
    {
        var assetSettings = new CreateAssetRequest(
            playbackPolicy: new List<PlaybackPolicy> { PlaybackPolicy.Signed },
            maxResolutionTier: CreateAssetRequest.MaxResolutionTierEnum._1080p);

        var request = new CreateUploadRequest(newAssetSettings: assetSettings)
        {
            CorsOrigin = _corsOrigin
        };

        try
        {
            var result = await _directUploadsApi.CreateDirectUploadAsync(request);
            return (result.Data.Url, result.Data.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MuxService] CreateDirectUploadAsync failed");
            throw;
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
            _logger.LogError(ex, "[MuxService] DeleteAssetAsync failed for {AssetId}", assetId);
        }
    }
}
