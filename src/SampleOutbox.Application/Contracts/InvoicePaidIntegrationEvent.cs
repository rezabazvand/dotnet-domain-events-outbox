using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Application.Contracts;

[EventKey("invoicing.invoice-paid.integration.v1")]
public sealed record InvoicePaidIntegrationEvent(
    Guid EventId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string PaymentReference,
    DateTime PaidAt,
    DateTime OccurredOn,
    int Version = 1);
