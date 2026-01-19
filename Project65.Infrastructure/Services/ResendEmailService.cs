using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project65.Core.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

namespace Project65.Infrastructure.Services;

public class ResendEmailService : IEmailService, IEmailSender
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly string _fromEmail;

    public ResendEmailService(IResend resend, IConfiguration configuration, ILogger<ResendEmailService> logger, ISettingsRepository settingsRepository)
    {
        _resend = resend;
        _logger = logger;
        _settingsRepository = settingsRepository;
        
        // Use a verified domain or the test 'onboarding@resend.dev' if strictly testing
        _fromEmail = configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";
    }

    public async Task SendEmailAsync(string to, string subject, string body, string? plainTextBody = null)
    {
        try
        {
            var storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "Project65 Studios";
            
            var message = new EmailMessage();
            message.From = $"{storeName} <{_fromEmail}>";
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;
            if (!string.IsNullOrEmpty(plainTextBody))
            {
                message.TextBody = plainTextBody;
            }
            
            _logger.LogInformation($"[Resend] Attempting to send '{subject}' to {to} from {_fromEmail}...");
            
            var response = await _resend.EmailSendAsync(message);

            if (response.Success)
            {
                 _logger.LogInformation($"[Resend] Email sent successfully. ID: {response.Content}");
            }
            else
            {
                 // Log warning and throw to ensure OrderFulfillmentService knows it failed
                 _logger.LogError($"[Resend] Failed to send email. Error: {response.Exception?.Message ?? "Unknown Error"}");
                 throw new Exception($"Resend API Error: {response.Exception?.Message ?? "Unknown Error"}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Resend] Exception while sending email to {to}");
            throw;
        }
    }

    Task IEmailSender.SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return SendEmailAsync(email, subject, htmlMessage, null);
    }
}
