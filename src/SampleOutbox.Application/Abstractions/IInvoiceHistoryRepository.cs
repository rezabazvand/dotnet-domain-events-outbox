using SampleOutbox.Domain.Invoices;

namespace SampleOutbox.Application.Abstractions;

public interface IInvoiceHistoryRepository
{
    void Add(InvoiceHistory history);
}
