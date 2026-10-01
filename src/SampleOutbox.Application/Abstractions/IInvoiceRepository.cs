using SampleOutbox.Domain.Invoices;

namespace SampleOutbox.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> FindAsync(Guid invoiceId, CancellationToken ct);

    void Add(Invoice invoice);
}
