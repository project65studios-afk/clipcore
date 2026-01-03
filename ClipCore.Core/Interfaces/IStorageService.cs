using System.IO;
using System.Threading.Tasks;

namespace ClipCore.Core.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadAsync(Stream stream, string fileName, string contentType);
        string GetPresignedDownloadUrl(string fileName, double durationMinutes = 60);
        string GetPresignedUploadUrl(string fileName, string contentType);
        Task<bool> FileExistsAsync(string fileName);
        Task DeleteAsync(string fileName);
        Task CopyFileAsync(string sourceKey, string destinationKey);
        Task ConfigureCorsAsync();
    }
}
