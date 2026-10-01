using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleOutbox.Application;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Application.Invoices;
using SampleOutbox.Domain.Invoices;
using SampleOutbox.Infrastructure;
using SampleOutbox.Infrastructure.Persistence;

var connection = new SqliteConnection("Filename=:memory:");
connection.Open();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSampleOutboxApplication();
builder.Services.AddSampleOutboxInfrastructure(options => options.UseSqlite(connection));
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
builder.Services.AddSingleton<IEventBus, ConsoleEventBus>();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

using var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleOutboxDbContext>();
    await db.Database.EnsureCreatedAsync();
}

Guid invoiceId;

await using (var scope = host.Services.CreateAsyncScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    var invoice = Invoice.Create(invoiceNumber: "INV-0001", customerEmail: "customer@example.com", amount: 120.50m);
    invoice.Issue();
    repository.Add(invoice);
    await unitOfWork.SaveChangesAsync();

    invoiceId = invoice.Id;
}

Section("Step 1 - State before payment");
await PrintStateAsync(host.Services, invoiceId);

Section("Step 2 - Call MarkAsPaid() and commit");
await using (var scope = host.Services.CreateAsyncScope())
{
    var payService = scope.ServiceProvider.GetRequiredService<PayInvoiceService>();
    await payService.PayAsync(invoiceId, paymentReference: "PAY-7781", paidAtUtc: DateTime.UtcNow, CancellationToken.None);
}
Console.WriteLine("Payment committed successfully.");

Section("Step 3 - Right after commit, before the outbox worker starts");
Console.WriteLine("Expected: the synchronous history row is already written,");
Console.WriteLine("but both outbox rows are still pending and no receipt has been emailed yet.");
await PrintStateAsync(host.Services, invoiceId);

Section("Step 4 - Start the worker (OutboxProcessor) and wait for the outbox to drain");
await host.StartAsync();
await WaitForOutboxToDrainAsync(host.Services);

Section("Step 5 - Final state");
await PrintStateAsync(host.Services, invoiceId);

await host.StopAsync();
connection.Close();

return;

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} ==");
}

static async Task PrintStateAsync(IServiceProvider services, Guid invoiceId)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SampleOutboxDbContext>();

    var invoice = await db.Invoices.AsNoTracking().FirstAsync(i => i.Id == invoiceId);
    Console.WriteLine($"Invoice {invoice.InvoiceNumber}: Status={invoice.Status}, Amount={invoice.Amount:0.00}, PaymentReference={invoice.PaymentReference ?? "-"}, PaidAt={invoice.PaidAt?.ToString("u") ?? "-"}");

    var historyCount = await db.InvoiceHistories.AsNoTracking().CountAsync(h => h.InvoiceId == invoiceId);
    Console.WriteLine($"InvoiceHistory rows: {historyCount}");

    var outboxRows = await db.OutboxMessages.AsNoTracking().OrderBy(m => m.OccurredOn).ToListAsync();
    if (outboxRows.Count == 0)
    {
        Console.WriteLine("Outbox: (empty)");
    }

    foreach (var row in outboxRows)
    {
        var status = row.IsDeadLettered ? "DEAD-LETTERED" : row.ProcessedOn is not null ? "PROCESSED" : "PENDING";
        Console.WriteLine($"  Outbox[{row.Kind}] key={row.PayloadKey} handler={row.HandlerKey ?? "-"} attempts={row.AttemptCount} status={status}");
    }
}

static async Task WaitForOutboxToDrainAsync(IServiceProvider services, TimeSpan? timeout = null)
{
    var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));

    while (DateTime.UtcNow < deadline)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SampleOutboxDbContext>();

        var pending = await db.OutboxMessages.CountAsync(m => m.ProcessedOn == null && !m.IsDeadLettered);
        if (pending == 0)
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(250));
    }

    Console.WriteLine("Warning: the outbox did not drain within the allowed time.");
}

internal sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, bool> _sentKeys = new();
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public Task SendAsync(string to, string subject, string body, string idempotencyKey, CancellationToken ct)
    {
        if (_sentKeys.ContainsKey(idempotencyKey))
        {
            Console.WriteLine($"[Email] duplicate send skipped for idempotencyKey={idempotencyKey}");
            return Task.CompletedTask;
        }

        var attempt = _attempts.AddOrUpdate(idempotencyKey, 1, (_, n) => n + 1);

        if (attempt == 1)
        {
            Console.WriteLine($"[Email] attempt {attempt} for idempotencyKey={idempotencyKey} -> simulated mail server failure");
            throw new InvalidOperationException("Simulated transient mail server failure.");
        }

        _sentKeys[idempotencyKey] = true;
        Console.WriteLine($"[Email] sent to {to}: \"{subject}\" / \"{body}\" (idempotencyKey={idempotencyKey}, attempt={attempt})");
        return Task.CompletedTask;
    }
}

internal sealed class ConsoleEventBus : IEventBus
{
    public Task PublishAsync(object integrationEvent, string eventKey, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        Console.WriteLine($"[EventBus] publish {eventKey}: {json}");
        return Task.CompletedTask;
    }
}
