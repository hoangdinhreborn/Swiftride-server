using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopes, IRawPublisher publisher, ILogger<OutboxPublisher> log) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try { await DrainAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogWarning(ex, "Lượt quét outbox lỗi, thử lại sau"); }
        }
        while (await WaitNextAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        OutboxMetrics.SetPending(await db.OutboxMessages.CountAsync(m => m.ProcessedAt == null, ct));

        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var m in batch)
        {
            // Span publish nằm trong trace của request đã sinh ra sự kiện
            ActivityContext.TryParse(m.TraceParent, null, out var parent);
            using var activity = MessagingTelemetry.Source.StartActivity(
                $"{m.RoutingKey} publish", ActivityKind.Producer, parent);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.operation", "publish");
            activity?.SetTag("messaging.rabbitmq.destination.routing_key", m.RoutingKey);

            try
            {
                await publisher.PublishAsync(m.RoutingKey, m.Type, Encoding.UTF8.GetBytes(m.Payload),
                    activity?.Id ?? m.TraceParent, ct);
                m.ProcessedAt = DateTime.UtcNow;
                m.LastError = null;
                log.LogInformation("Outbox: đã gửi {Type} {Id}", m.Type, m.Id);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                m.Attempts++;
                m.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                log.LogWarning("Outbox: chưa gửi được {Type} {Id} (lần {Attempts}): {Error}", m.Type, m.Id, m.Attempts, ex.Message);
                await db.SaveChangesAsync(CancellationToken.None);
                return;   // giữ thứ tự
            }
        }

        await db.SaveChangesAsync(ct);
    }
}