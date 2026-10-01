using SampleOutbox.Application.Abstractions;
using SampleOutbox.Application.Contracts;
using SampleOutbox.Domain.Invoices.Events;

namespace SampleOutbox.Application.Invoices.Handlers;

public sealed class PublishInvoicePaidHandler(IOutbox outbox) : IDomainEventHandler<InvoicePaidDomainEvent>
{
    public Task Handle(InvoicePaidDomainEvent e, CancellationToken ct)
    {
        var integrationEvent = new InvoicePaidIntegrationEvent(
            EventId: e.EventId,
            InvoiceId: e.InvoiceId,
            InvoiceNumber: e.InvoiceNumber,
            Amount: e.Amount,
            PaymentReference: e.PaymentReference,
            PaidAt: e.PaidAt,
            OccurredOn: e.OccurredOn);

        outbox.Add(OutboxKind.IntegrationEvent, integrationEvent);

        return Task.CompletedTask;
    }
}
