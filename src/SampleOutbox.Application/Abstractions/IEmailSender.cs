namespace SampleOutbox.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, string idempotencyKey, CancellationToken ct);
}
