using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project65.Core.Interfaces;

using Microsoft.AspNetCore.Identity.UI.Services;

namespace Project65.Infrastructure.Services;

public class AmazonSESEmailService : IEmailService, IEmailSender
{
    private readonly IAmazonSimpleEmailService? _sesClient;
    private readonly ILogger<AmazonSESEmailService> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly string _fromEmail;

    public AmazonSESEmailService(IConfiguration configuration, ILogger<AmazonSESEmailService> logger, ISettingsRepository settingsRepository)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        
        // Amazon SES Configuration
        var accessKey = configuration["AWS:AccessKeyId"];
        var secretKey = configuration["AWS:SecretAccessKey"];
        var regionStr = configuration["AWS:Region"] ?? "us-east-1";
        
        _fromEmail = configuration["AWS:FromEmail"] ?? "no-reply@project65.com";

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("AWS SES Access Keys are missing (AWS:AccessKeyId, AWS:SecretAccessKey). Email functionality will be DISABLED.");
            _sesClient = null;
            return;
        }

        try 
        {
            var region = RegionEndpoint.GetBySystemName(regionStr);
            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            
            // Explicitly create client to avoid conflict with global R2 options
            _sesClient = new AmazonSimpleEmailServiceClient(credentials, region);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to initialize AWS SES Client.");
             _sesClient = null;
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            if (_sesClient == null)
            {
                _logger.LogWarning($"[SES] Email sending skipped (Client not configured). Subject: '{subject}', To: {to}");
                return;
            }

            var storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "Project65 Studios";
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
