using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TripService.Application.Common.Exceptions;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips.Commands.ApplyPaymentResult;
using TripService.Application.Trips.Dtos;
using TripService.Domain.Common;
using System.Diagnostics;

namespace TripService.Infrastructure.Messaging;

public sealed record RabbitMqSettings(string Uri);

public sealed class PaymentResultConsumer(
    RabbitMqSettings settings, IServiceScopeFactory scopes, ILogger<PaymentResultConsumer> log) : BackgroundService
{
    private const string Exchange = "swiftride.events";
    private const string Queue = "trip.payment-results";
    private const string SettledKey = "payment.settled";
    private const string FailedKey = "payment.failed";
    private const string DeadLetterExchange = "swiftride.trip.dlx";   // riêng của Trip, không lẫn với DLQ của Payment
    private const string DeadLetterQueue = "trip.payment-results.dlq";
    private const int MaxAttempts = 3;

    // Hợp đồng sự kiện: bản riêng của Trip, chỉ lấy các trường cần dùng
    private sealed record PaymentMessage(Guid EventId, Guid PaymentId, Guid TripId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "", cancellationToken: ct);

        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange },
            cancellationToken: ct);
        await channel.QueueBindAsync(Queue, Exchange, SettledKey, cancellationToken: ct);
        await channel.QueueBindAsync(Queue, Exchange, FailedKey, cancellationToken: ct);

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
            var msg = JsonSerializer.Deserialize<PaymentMessage>(ea.Body.Span, JsonSerializerOptions.Web)
                      ?? throw new JsonException("Message rỗng");

            await ProcessWithRetryAsync(msg, succeeded: ea.RoutingKey == SettledKey, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            log.LogError(ex, "Không xử lý được message {RoutingKey}, chuyển vào DLQ", ea.RoutingKey);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    private async Task ProcessWithRetryAsync(PaymentMessage msg, bool succeeded, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<ApplyPaymentResultCommand, ApplyPaymentResultOutcome>>();

                var outcome = await handler.HandleAsync(new ApplyPaymentResultCommand(msg.TripId, msg.PaymentId, succeeded), ct);

                if (outcome.Applied)
                    log.LogInformation("Chuyến {TripId}: thanh toán {Result} -> trạng thái {Status}",
                        msg.TripId, succeeded ? "thành công" : "thất bại", outcome.TripStatus);
                else
                    log.LogWarning("Chuyến {TripId}: không áp dụng được thanh toán {PaymentId} ({Reason}). {Action}",
                        msg.TripId, msg.PaymentId, outcome.RejectionReason,
                        succeeded ? "Đã phát trip.payment-rejected để hoàn tiền" : "Chưa trừ tiền nên không cần bồi thường");
                return;
            }
            // Thử lại cũng vô ích: để người xử lý qua DLQ
            catch (Exception ex) when (ex is NotFoundException or DomainException) { throw; }
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested)   // ví dụ ConcurrencyException
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                log.LogWarning(ex, "Lần {Attempt}/{Max} thất bại, thử lại sau {Delay}", attempt, MaxAttempts, delay);
                await Task.Delay(delay, ct);
            }
        }
    }
}