using Amazon.S3;
using Amazon.S3.Model;
using ClipCore.API.Interfaces;

namespace ClipCore.API.Services;

public class R2StorageService : IR2StorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(IConfiguration config, ILogger<R2StorageService> logger)
    {
        _logger = logger;
        _bucket = config["R2:BucketName"] ?? throw new InvalidOperationException("R2:BucketName not configured");

        var accountId       = config["R2:AccountId"]       ?? throw new InvalidOperationException("R2:AccountId not configured");
        var accessKeyId     = config["R2:AccessKeyId"]     ?? throw new InvalidOperationException("R2:AccessKeyId not configured");
        var secretAccessKey = config["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey not configured");

        var s3Config = new AmazonS3Config
        {
            ServiceURL        = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle    = true,
            SignatureVersion  = "4"
        };

        _s3 = new AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
    }

    public async Task DeleteAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        try
        {
            await _s3.DeleteObjectAsync(_bucket, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[R2] DeleteAsync failed for {Key}", key);
        }
    }

    public string GetPresignedUploadUrl(string key, string contentType)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName      = _bucket,
                Key             = key,
                Expires         = DateTime.UtcNow.AddMinutes(60),
                Verb            = HttpVerb.PUT,
                ContentType     = contentType
            };
            return _s3.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[R2] GetPresignedUploadUrl failed for {Key}", key);
            return "";
        }
    }
}
