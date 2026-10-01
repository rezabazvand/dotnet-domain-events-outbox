using Microsoft.Extensions.DependencyInjection;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Application.Invoices;
using SampleOutbox.Application.Invoices.Handlers;
using SampleOutbox.Domain.Invoices.Events;

namespace SampleOutbox.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSampleOutboxApplication(this IServiceCollection services)
    {
        services.AddScoped<PayInvoiceService>();

        services.AddScoped<RecordInvoiceHistoryHandler>();
        services.AddScoped<IDomainEventHandler<InvoicePaidDomainEvent>>(sp => sp.GetRequiredService<RecordInvoiceHistoryHandler>());

        services.AddScoped<SendReceiptEmailHandler>();
        services.AddScoped<IDomainEventHandler<InvoicePaidDomainEvent>>(sp => sp.GetRequiredService<SendReceiptEmailHandler>());

        services.AddScoped<PublishInvoicePaidHandler>();
        services.AddScoped<IDomainEventHandler<InvoicePaidDomainEvent>>(sp => sp.GetRequiredService<PublishInvoicePaidHandler>());

        return services;
    }
}
