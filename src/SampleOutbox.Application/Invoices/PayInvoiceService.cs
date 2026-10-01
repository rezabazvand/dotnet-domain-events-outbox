using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Application.Invoices;

public sealed class PayInvoiceService(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
{
    public async Task PayAsync(Guid invoiceId, string paymentReference, DateTime paidAtUtc, CancellationToken ct)
    {
        var invoice = await invoiceRepository.FindAsync(invoiceId, ct)
            ?? throw new DomainException($"Invoice {invoiceId} was not found.");

        invoice.MarkAsPaid(paymentReference, paidAtUtc);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
