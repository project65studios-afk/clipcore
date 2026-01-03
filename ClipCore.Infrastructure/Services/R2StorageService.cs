using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClipCore.Core.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClipCore.Infrastructure.Services
{
    public class R2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly IConfiguration _configuration;
        private readonly ILogger<R2StorageService> _logger;

        public R2StorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<R2StorageService> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _bucketName = configuration["R2:BucketName"] ?? throw new ArgumentNullException("R2:BucketName configuration is missing");
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
        {
            try
            {
                var putRequest = new PutObjectRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = _bucketName,
                    ContentType = contentType,
                    AutoCloseStream = false, // Critical for keeping stream open if needed elsewhere
                    DisablePayloadSigning = true // Optimization for R2
                };

                _logger.LogInformation($"[R2] Starting upload for {fileName} to {_bucketName}");
                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation($"[R2] Upload complete: {fileName}");

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Upload failed for {fileName}");
                throw;
            }
        }

        public string GetPresignedDownloadUrl(string fileName, double durationMinutes = 60)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    Expires = DateTime.UtcNow.AddMinutes(durationMinutes),
                    Verb = HttpVerb.GET
                };

                var url = _s3Client.GetPreSignedURL(request);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Failed to generate download URL for {fileName}");
                return "";
            }
        }

        public string GetPresignedUploadUrl(string fileName, string contentType)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    Expires = DateTime.UtcNow.AddMinutes(60), // 1 hour to upload
                    Verb = HttpVerb.PUT,
                    ContentType = contentType
                };

                return _s3Client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Failed to generate upload URL for {fileName}");
                return "";
            }
        }

        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_bucketName, fileName);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Error checking existence for {fileName}");
                return false;
            }
        }

        public async Task DeleteAsync(string fileName)
        {
            try
            {
                _logger.LogInformation($"[R2] Deleting {fileName} from {_bucketName}");
                await _s3Client.DeleteObjectAsync(_bucketName, fileName);
                _logger.LogInformation($"[R2] Deletion complete: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Delete failed for {fileName}");
                // We don't necessarily want to crash the whole deletion flow if storage cleanup fails, 
                // but logging it is critical.
            }
        }

        public async Task CopyFileAsync(string sourceKey, string destinationKey)
        {
            try
            {
                var request = new CopyObjectRequest
                {
                    SourceBucket = _bucketName,
                    SourceKey = sourceKey,
                    DestinationBucket = _bucketName,
                    DestinationKey = destinationKey
                };

                _logger.LogInformation($"[R2] Copying from {sourceKey} to {destinationKey}");
                await _s3Client.CopyObjectAsync(request);
                _logger.LogInformation($"[R2] Copy complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[R2] Copy failed from {sourceKey} to {destinationKey}");
                throw;
            }
        }

        public async Task ConfigureCorsAsync()
        {
            try
            {
                var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>() 
                                     ?? new[] { "http://localhost:5094", "http://127.0.0.1:5094", "https://localhost:7192" };

                var corsRules = new System.Collections.Generic.List<CORSRule>
                {
                    new CORSRule
                    {
                        AllowedOrigins = new System.Collections.Generic.List<string>(allowedOrigins),
                        AllowedMethods = new System.Collections.Generic.List<string> { "GET", "HEAD", "PUT", "POST" },
                        AllowedHeaders = new System.Collections.Generic.List<string> { "*" },
                        ExposeHeaders = new System.Collections.Generic.List<string> { "ETag", "Content-Length", "Content-Range" },
                        MaxAgeSeconds = 3000
                    }
                };

                var request = new PutCORSConfigurationRequest
                {
                    BucketName = _bucketName,
                    Configuration = new CORSConfiguration { Rules = corsRules }
                };

                await _s3Client.PutCORSConfigurationAsync(request);
                _logger.LogInformation("[R2] CORS configuration updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[R2] Failed to update CORS configuration.");
            }
        }
    }
}
