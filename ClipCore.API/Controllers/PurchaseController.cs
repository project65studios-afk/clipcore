using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace ClipCore.API.Controllers;

[ApiController]
public class PurchaseController : ControllerBase
{
    private readonly IPurchaseData   _purchaseData;
    private readonly IConfiguration  _config;

    public PurchaseController(IPurchaseData purchaseData, IConfiguration config)
    {
        _purchaseData = purchaseData;
        _config       = config;
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured");
    }

    // ── Buyer: list own purchases ──────────────────────────────────────────────

    [Authorize] [HttpGet("GetMyPurchases")]
    public async Task<IActionResult> GetMyPurchases()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items  = await _purchaseData.GetPurchasesByUser(userId);
        return Ok(items);
    }

    // ── Seller: list their own purchases ──────────────────────────────────────

    [Authorize(Roles = "Seller")] [HttpGet("GetSellerPurchases")]
    public async Task<IActionResult> GetSellerPurchases()
    {
        var sellerId = int.Parse(User.FindFirstValue("seller_id")!);
        var items    = await _purchaseData.GetPurchasesBySeller(sellerId);
        return Ok(items);
    }

    // ── Checkout: create Stripe session ───────────────────────────────────────

    [HttpPost("CreateCheckout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest req)
    {
        if (req.Items == null || req.Items.Count == 0) return BadRequest("Cart is empty.");

        var userId    = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var lineItems = new List<SessionLineItemOptions>();
        var sessionMeta = new Dictionary<string, string>();

        for (int i = 0; i < req.Items.Count; i++)
        {
            var item = req.Items[i];

            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency    = "usd",
                    UnitAmount  = item.PriceCents,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name     = item.Title,
                        Metadata = new Dictionary<string, string>
                        {
                            ["ClipId"]      = item.Id,
                            ["LicenseType"] = item.LicenseType.ToString(),
                            ["IsGif"]       = item.IsGif.ToString(),
                        },
                    },
                },
                Quantity = 1,
            });

            // Backup metadata at session level (in case line-item metadata is inaccessible in webhook)
            sessionMeta[$"c{i}_id"]  = item.Id;
            sessionMeta[$"c{i}_li"]  = item.LicenseType.ToString();
            sessionMeta[$"c{i}_gif"] = item.IsGif.ToString();
            sessionMeta[$"c{i}_p"]   = item.PriceCents.ToString();
            if (!string.IsNullOrEmpty(item.MasterFileName)) sessionMeta[$"c{i}_mf"] = item.MasterFileName;
            if (item.GifStartTime.HasValue)  sessionMeta[$"c{i}_gs"] = item.GifStartTime.Value.ToString("F2");
            if (item.GifEndTime.HasValue)    sessionMeta[$"c{i}_ge"] = item.GifEndTime.Value.ToString("F2");
        }
        sessionMeta["count"] = req.Items.Count.ToString();

        var options = new SessionCreateOptions
        {
            Mode              = "payment",
            LineItems         = lineItems,
            SuccessUrl        = req.SuccessUrl,
            CancelUrl         = req.CancelUrl,
            ClientReferenceId = userId,
            CustomerEmail     = userEmail,
            Metadata          = sessionMeta,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = sessionMeta,
            },
        };

        if (!string.IsNullOrEmpty(req.PromoCode))
        {
            // Let Stripe handle promos via coupons if configured. Skip for now.
        }

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return Ok(new { checkoutUrl = session.Url });
    }
}

// ── Request model ─────────────────────────────────────────────────────────────

public class CreateCheckoutRequest
{
    public List<CheckoutCartItem> Items   { get; set; } = new();
    public string SuccessUrl              { get; set; } = "";
    public string CancelUrl               { get; set; } = "";
    public string? PromoCode              { get; set; }
}

public class CheckoutCartItem
{
    public string      Id           { get; set; } = "";
    public string      Title        { get; set; } = "";
    public string      CollectionId { get; set; } = "";
    public string      CollectionName { get; set; } = "";
    public int         PriceCents   { get; set; }
    public LicenseType LicenseType  { get; set; } = LicenseType.Personal;
    public bool        IsGif        { get; set; }
    public double?     GifStartTime { get; set; }
    public double?     GifEndTime   { get; set; }
    public string?     MasterFileName { get; set; }
}
