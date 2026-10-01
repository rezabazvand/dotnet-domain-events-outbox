using SampleOutbox.Domain.Invoices.Events;
using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Domain.Invoices;

public sealed class Invoice : Entity
{
    public Guid Id { get; private set; }

    public string InvoiceNumber { get; private set; } = null!;

    public string CustomerEmail { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public string? PaymentReference { get; private set; }

    public DateTime? PaidAt { get; private set; }

    private Invoice()
    {
    }

    public static Invoice Create(string invoiceNumber, string customerEmail, decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Invoice amount must be greater than zero.");
        }

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            CustomerEmail = customerEmail,
            Amount = amount,
            Status = InvoiceStatus.Draft
        };
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException($"Invoice {InvoiceNumber} cannot be issued from status {Status}.");
        }

        Status = InvoiceStatus.Issued;
    }

    public void MarkAsPaid(string paymentReference, DateTime paidAtUtc)
    {
        if (Status == InvoiceStatus.Paid)
        {
            return;
        }

        if (Status != InvoiceStatus.Issued)
        {
            throw new DomainException(
                $"Invoice {InvoiceNumber} cannot be paid from status {Status}; it must be {InvoiceStatus.Issued} first.");
        }

        var previousStatus = Status;

        Status = InvoiceStatus.Paid;
        PaymentReference = paymentReference;
        PaidAt = paidAtUtc;

        Raise(new InvoicePaidDomainEvent(
            EventId: Guid.NewGuid(),
            InvoiceId: Id,
            InvoiceNumber: InvoiceNumber,
            CustomerEmail: CustomerEmail,
            Amount: Amount,
            PreviousStatus: previousStatus,
            PaymentReference: paymentReference,
            PaidAt: paidAtUtc,
            OccurredOn: DateTime.UtcNow));
    }
}
