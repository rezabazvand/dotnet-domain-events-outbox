namespace SampleOutbox.Domain.Invoices;

public sealed class InvoiceHistory
{
    public Guid Id { get; private set; }

    public Guid InvoiceId { get; private set; }

    public InvoiceStatus FromStatus { get; private set; }

    public InvoiceStatus ToStatus { get; private set; }

    public string? Note { get; private set; }

    public DateTime ChangedAt { get; private set; }

    private InvoiceHistory()
    {
    }

    public static InvoiceHistory Record(Guid invoiceId, InvoiceStatus fromStatus, InvoiceStatus toStatus, string? note, DateTime changedAt)
    {
        return new InvoiceHistory
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Note = note,
            ChangedAt = changedAt
        };
    }
}
