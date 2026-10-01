using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Invoices;
using SampleOutbox.Domain.Invoices.Events;

namespace SampleOutbox.Application.Invoices.Handlers;

public sealed class RecordInvoiceHistoryHandler(IInvoiceHistoryRepository historyRepository)
    : IDomainEventHandler<InvoicePaidDomainEvent>
{
    public Task Handle(InvoicePaidDomainEvent e, CancellationToken ct)
    {
        var history = InvoiceHistory.Record(
            e.InvoiceId,
            e.PreviousStatus,
            InvoiceStatus.Paid,
            note: $"Paid with reference {e.PaymentReference}",
            changedAt: e.OccurredOn);

        historyRepository.Add(history);

        return Task.CompletedTask;
    }
}
