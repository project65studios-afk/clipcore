namespace ClipCore.API.Interfaces;

public interface IMuxService
{
    Task<(string uploadUrl, string uploadId)> CreateDirectUploadAsync();
    Task DeleteAssetAsync(string assetId);
}
