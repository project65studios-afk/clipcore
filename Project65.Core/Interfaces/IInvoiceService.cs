using Project65.Core.Entities;

namespace Project65.Core.Interfaces;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoiceAsync(Purchase purchase);
}
