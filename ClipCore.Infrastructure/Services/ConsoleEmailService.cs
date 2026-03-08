using Microsoft.Extensions.Logging;
using ClipCore.Core.Interfaces;

using Microsoft.AspNetCore.Identity.UI.Services;

namespace ClipCore.Infrastructure.Services;

public class ConsoleEmailService : IEmailService, IEmailSender
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, string? plainTextBody = null)
    {
        _logger.LogInformation("--------------------------------------------------");
        _logger.LogInformation($"[Email Mock] To: {to}");
        _logger.LogInformation($"[Email Mock] Subject: {subject}");
        _logger.LogInformation($"[Email Mock] HTML Body Length: {body.Length}");
        if (!string.IsNullOrEmpty(plainTextBody))
        {
             _logger.LogInformation($"[Email Mock] Text Body: {plainTextBody}");
        }
        _logger.LogInformation("--------------------------------------------------");
        return Task.CompletedTask;
    }
    Task IEmailSender.SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return SendEmailAsync(email, subject, htmlMessage, null);
    }
}
