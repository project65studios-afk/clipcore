namespace ClipCore.API.Interfaces;

public interface IR2StorageService
{
    Task DeleteAsync(string key);
    string GetPresignedUploadUrl(string key, string contentType);
}
