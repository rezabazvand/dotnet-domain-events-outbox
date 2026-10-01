using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Invoices.Events;
using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Application.Invoices.Handlers;

[AsyncHandler]
[EventKey("invoicing.handler.receipt-email")]
public sealed class SendReceiptEmailHandler(IEmailSender emailSender) : IDomainEventHandler<InvoicePaidDomainEvent>
{
    public async Task Handle(InvoicePaidDomainEvent e, CancellationToken ct)
    {
        var subject = $"Receipt for invoice {e.InvoiceNumber}";
        var body = $"Thanks for your payment of {e.Amount:0.00}. Reference: {e.PaymentReference}.";

        await emailSender.SendAsync(e.CustomerEmail, subject, body, idempotencyKey: e.EventId.ToString(), ct);
    }
}
