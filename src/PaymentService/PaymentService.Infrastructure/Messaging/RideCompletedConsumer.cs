using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Commands.ProcessRideCompleted;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Domain.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace PaymentService.Infrastructure.Messaging;

public sealed record RabbitMqSettings(string Uri);

public sealed class RideCompletedConsumer(
    RabbitMqSettings settings, IServiceScopeFactory scopes, ILogger<RideCompletedConsumer> log) : BackgroundService
{
    private const string Exchange = "swiftride.events";
    private const string RoutingKey = "ride.completed";
    private const string Queue = "payment.ride-completed";
    private const string DeadLetterExchange = "swiftride.events.dlx";
    private const string DeadLetterQueue = "payment.ride-completed.dlq";
    private const int MaxAttempts = 3;

    // Hợp đồng sự kiện: bản riêng của Payment, không dùng chung code với Trip
    private sealed record RideCompletedMessage(
        Guid EventId, Guid TripId, Guid RiderId, Guid DriverId, decimal Amount, DateTime OccurredAt);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ chưa sẵn sàng lúc khởi động thì thử lại, không làm sập service
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogWarning(ex, "Mất kết nối RabbitMQ, thử lại sau 5 giây"); }

            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(settings.Uri),
            AutomaticRecoveryEnabled = true,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        // Dead-letter: message bị nack (requeue = false) sẽ rơi vào hàng đợi DLQ
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "", cancellationToken: ct);

        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange },
            cancellationToken: ct);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey, cancellationToken: ct);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => OnMessageAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: ct);

        log.LogInformation("Đang nghe hàng đợi {Queue}", Queue);
        await Task.Delay(Timeout.Infinite, ct);
    }

        private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        using var activity = MessagingTelemetry.StartConsumer(ea.RoutingKey, ea.BasicProperties);
        try
        {
            var msg = JsonSerializer.Deserialize<RideCompletedMessage>(ea.Body.Span, JsonSerializerOptions.Web)
                      ?? throw new JsonException("Message rỗng");

            await ProcessWithRetryAsync(msg, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            log.LogError(ex, "Không xử lý được message, chuyển vào DLQ");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    private async Task ProcessWithRetryAsync(RideCompletedMessage msg, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();   // mỗi lần thử một scope (một DbContext) mới
                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<ProcessRideCompletedCommand, PaymentDto>>();

                var result = await handler.HandleAsync(
                    new ProcessRideCompletedCommand(msg.EventId, msg.TripId, msg.RiderId, msg.Amount), ct);

                log.LogInformation("Chuyến {TripId}: thanh toán {PaymentId} -> {Status}",
                    msg.TripId, result.Id, result.Status);
                return;
            }
            catch (DomainValidationException) { throw; }   // dữ liệu sai: thử lại vô ích, vào DLQ ngay
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                log.LogWarning(ex, "Lần {Attempt}/{Max} thất bại, thử lại sau {Delay}", attempt, MaxAttempts, delay);
                await Task.Delay(delay, ct);
            }
        }
    }
}