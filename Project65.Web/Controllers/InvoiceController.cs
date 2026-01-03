using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project65.Core.Entities;
using Project65.Core.Interfaces;

namespace Project65.Web.Controllers;

[Authorize]
[IgnoreAntiforgeryToken]
[Route("api/invoices")]
[ApiController]
public class InvoiceController : ControllerBase
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoiceController(
        IPurchaseRepository purchaseRepository,
        IInvoiceService invoiceService,
        UserManager<ApplicationUser> userManager)
    {
        _purchaseRepository = purchaseRepository;
        _invoiceService = invoiceService;
        _userManager = userManager;
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetInvoice(string orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var purchases = await _purchaseRepository.GetByUserIdAsync(user.Id);
        var purchase = purchases.FirstOrDefault(p => p.OrderId == orderId);

        if (purchase == null)
        {
            return NotFound("Order not found or access denied.");
        }

        var pdfBytes = await _invoiceService.GenerateInvoiceAsync(purchase);

        return File(pdfBytes, "application/pdf", $"Invoice_{orderId}.pdf");
    }
}
