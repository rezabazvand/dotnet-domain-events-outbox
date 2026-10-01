using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Domain.Invoices.Events;

[EventKey("invoicing.invoice.paid.v1")]
public sealed record InvoicePaidDomainEvent(
    Guid EventId,
    Guid InvoiceId,
    string InvoiceNumber,
    string CustomerEmail,
    decimal Amount,
    InvoiceStatus PreviousStatus,
    string PaymentReference,
    DateTime PaidAt,
    DateTime OccurredOn) : IDomainEvent;
