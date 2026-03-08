using ClipCore.Core.Entities;

namespace ClipCore.Core.Interfaces;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoiceAsync(Purchase purchase);
}
