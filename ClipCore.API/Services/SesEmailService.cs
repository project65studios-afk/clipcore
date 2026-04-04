using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using ClipCore.API.Interfaces;

namespace ClipCore.API.Services;

public class SesEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SesEmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var client = new AmazonSimpleEmailServiceClient(
                _config["AWS:AccessKeyId"],
                _config["AWS:SecretAccessKey"],
                Amazon.RegionEndpoint.GetBySystemName(_config["AWS:Region"] ?? "us-east-1"));

            await client.SendEmailAsync(new SendEmailRequest
            {
                Source      = _config["AWS:FromEmail"],
                Destination = new Destination { ToAddresses = new List<string> { to } },
                Message     = new Message
                {
                    Subject = new Content(subject),
                    Body    = new Body { Html = new Content { Charset = "UTF-8", Data = htmlBody } }
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SesEmailService] {ex.Message}");
        }
    }
}
