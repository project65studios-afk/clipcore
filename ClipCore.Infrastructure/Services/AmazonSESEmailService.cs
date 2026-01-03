using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClipCore.Core.Interfaces;

namespace ClipCore.Infrastructure.Services;

public class AmazonSESEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ILogger<AmazonSESEmailService> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly string _fromEmail;

    public AmazonSESEmailService(IConfiguration configuration, ILogger<AmazonSESEmailService> logger, ISettingsRepository settingsRepository)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        
        // Amazon SES Configuration
        // distinct from R2 configuration
        var accessKey = configuration["AWS:AccessKeyId"];
        var secretKey = configuration["AWS:SecretAccessKey"];
        var regionStr = configuration["AWS:Region"] ?? "us-east-1";
        
        _fromEmail = configuration["AWS:FromEmail"] ?? "no-reply@project65.com";

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("AWS SES Access Keys are missing. Email service will fail.");
            // We initialize with a dummy or throw. Let's throw to be explicit during dev.
            // But to avoid crashing everything if just email is misconfigured, we'll log.
            // However, we can't create the client without keys.
            throw new ArgumentNullException("AWS SES credentials missing in configuration (AWS:AccessKeyId, AWS:SecretAccessKey)");
        }

        var region = RegionEndpoint.GetBySystemName(regionStr);
        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        
        // Explicitly create client to avoid conflict with global R2 options
        _sesClient = new AmazonSimpleEmailServiceClient(credentials, region);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "ClipCore Studios";
            var sendRequest = new SendEmailRequest
            {
                Source = $"{storeName} <{_fromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { to }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = body
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            // Simple strip of HTML for text fallback, or meaningful fallback
                            Data = "Please view this email in an HTML-compatible client."
                        }
                    }
                }
            };

            _logger.LogInformation($"[SES] Sending email to {to} with subject '{subject}'");
            var response = await _sesClient.SendEmailAsync(sendRequest);
            _logger.LogInformation($"[SES] Email sent. Message ID: {response.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SES] Failed to send email to {to}");
            throw; // Rethrow to let caller handle or display error
        }
    }
}
