using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using ClipCore.Web.Services;

namespace ClipCore.Web.Controllers;

[Route("api/webhooks")]
[ApiController]
public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly OrderFulfillmentService _fulfillmentService;

    public StripeWebhookController(
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger,
        OrderFulfillmentService fulfillmentService)
    {
        _configuration = configuration;
        _logger = logger;
        _fulfillmentService = fulfillmentService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            // Verify signature using the secret from configuration (or Env Var)
            // Ideally 'Stripe:WebhookSecret', fallback to direct check
            var endpointSecret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(endpointSecret))
            {
                // In dev, we might accept without signature if explicitly allowed, but better to be strict
                _logger.LogWarning("Stripe Webhook Secret is missing from configuration.");
                return BadRequest("Configuration Error");
            }

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );

            // Handle the event
            _logger.LogInformation($"[StripeWebhook] Received Event: {stripeEvent.Type} | ID: {stripeEvent.Id}");

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null)
                {
                    _logger.LogInformation($"[StripeWebhook] Processing Session: {session.Id}");
                    await _fulfillmentService.FulfillOrderAsync(session.Id);
                    _logger.LogInformation($"[StripeWebhook] Successfully fulfilled order for session: {session.Id}");
                }
            }
            else
            {
                // Just log other events
                _logger.LogInformation($"[StripeWebhook] Unhandled event type: {stripeEvent.Type}");
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe Webhook Error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General Webhook Error");
            return StatusCode(500);
        }
    }
}
