using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Invoices;

namespace SampleOutbox.Infrastructure.Persistence;

public sealed class InvoiceHistoryRepository(SampleOutboxDbContext db) : IInvoiceHistoryRepository
{
    public void Add(InvoiceHistory history)
    {
        db.InvoiceHistories.Add(history);
    }
}
