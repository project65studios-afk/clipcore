using Microsoft.Extensions.Logging;
using Project65.Core.Interfaces;

namespace Project65.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("--------------------------------------------------");
        _logger.LogInformation($"[Email Mock] To: {to}");
        _logger.LogInformation($"[Email Mock] Subject: {subject}");
        _logger.LogInformation($"[Email Mock] Body: {body}");
        _logger.LogInformation("--------------------------------------------------");
        return Task.CompletedTask;
    }
}
