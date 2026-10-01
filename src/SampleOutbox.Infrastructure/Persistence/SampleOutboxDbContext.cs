using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Invoices;
using SampleOutbox.Infrastructure.Events;
using SampleOutbox.Infrastructure.Outbox;

namespace SampleOutbox.Infrastructure.Persistence;

public sealed class SampleOutboxDbContext(DbContextOptions<SampleOutboxDbContext> options, IServiceProvider serviceProvider)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceHistory> InvoiceHistories => Set<InvoiceHistory>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var dispatcher = serviceProvider.GetRequiredService<DomainEventDispatcher>();
        await dispatcher.DispatchAsync(this, ct);
        return await base.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesWithoutDispatchAsync(CancellationToken ct = default)
    {
        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Status).HasConversion<int>();
            builder.Property(i => i.Amount).HasConversion<string>();
            builder.HasIndex(i => i.InvoiceNumber).IsUnique();
            builder.Ignore(i => i.DomainEvents);
        });

        modelBuilder.Entity<InvoiceHistory>(builder =>
        {
            builder.HasKey(h => h.Id);
            builder.Property(h => h.FromStatus).HasConversion<int>();
            builder.Property(h => h.ToStatus).HasConversion<int>();
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Kind).HasConversion<int>();
            builder.HasIndex(m => new { m.ProcessedOn, m.IsDeadLettered, m.NextAttemptOn });
        });
    }
}
