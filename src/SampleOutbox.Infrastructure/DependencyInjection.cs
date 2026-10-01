using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Application.Invoices;
using SampleOutbox.Domain.Primitives;
using SampleOutbox.Infrastructure.Events;
using SampleOutbox.Infrastructure.Outbox;
using SampleOutbox.Infrastructure.Persistence;

namespace SampleOutbox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSampleOutboxInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddSingleton(new EventTypeRegistry([
            typeof(IDomainEvent).Assembly,
            typeof(PayInvoiceService).Assembly
        ]));

        services.AddDbContext<SampleOutboxDbContext>(configureDb, ServiceLifetime.Scoped);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SampleOutboxDbContext>());

        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceHistoryRepository, InvoiceHistoryRepository>();

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
