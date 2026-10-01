using Microsoft.EntityFrameworkCore;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Invoices;

namespace SampleOutbox.Infrastructure.Persistence;

public sealed class InvoiceRepository(SampleOutboxDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> FindAsync(Guid invoiceId, CancellationToken ct)
    {
        return db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
    }

    public void Add(Invoice invoice)
    {
        db.Invoices.Add(invoice);
    }
}
