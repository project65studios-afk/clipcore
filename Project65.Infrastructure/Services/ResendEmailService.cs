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

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "Project65 Studios";
            
            var message = new EmailMessage();
            message.From = $"{storeName} <{_fromEmail}>";
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;
            
            _logger.LogInformation($"[Resend] Sending email to {to} with subject '{subject}' from {_fromEmail}");
            
            var response = await _resend.EmailSendAsync(message);

            if (response.Success)
            {
                 _logger.LogInformation($"[Resend] Email sent successfully. ID: {response.Data.Id}");
            }
            else
            {
                 // Log warning but don't crash, or throw? 
                 // It's a void-ish async task usually, but let's log error.
                 _logger.LogError($"[Resend] Failed to send email. Error: {response.Error?.Message} ({response.Error?.Name})");
                 // We might want to throw if it's critical, but avoiding crash is often better for UI flow
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Resend] Exception while sending email to {to}");
            throw;
        }
    }
}
