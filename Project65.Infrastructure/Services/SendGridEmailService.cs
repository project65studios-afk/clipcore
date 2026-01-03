using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project65.Core.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Project65.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly string _fromEmail;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger, ISettingsRepository settingsRepository)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        var apiKey = configuration["SendGrid:ApiKey"];
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "no-reply@project65.com";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SendGrid API Key missing. Service will fail to send.");
            // We could throw, but might be safer to just log and let it fail gracefully or init client with null
             // Actually if using standard DI, we prefer to error early or handle it.
             // We'll proceed, assuming Program.cs only registers this if key exists.
        }
        _client = new SendGridClient(apiKey);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var storeName = await _settingsRepository.GetValueAsync("StoreName") ?? "Project65 Studios";
        var from = new EmailAddress(_fromEmail, storeName);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body); // Plain text and HTML same for now

        try 
        {
            var response = await _client.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"[SendGrid] Email sent to {to} (Subject: {subject})");
            }
            else
            {
                _logger.LogError($"[SendGrid] Failed to send email to {to}. Status: {response.StatusCode}");
                var bodyContent = await response.Body.ReadAsStringAsync();
                _logger.LogError($"[SendGrid] Error Body: {bodyContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SendGrid] Exception sending to {to}");
            throw; // Re-throw so UI knows? Or suppress? Suppress is safer for user experience (don't crack the page)
        }
    }
}
