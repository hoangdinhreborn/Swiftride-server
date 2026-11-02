using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Commands.CompensatePayment;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Domain.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure.Messaging;

public sealed class PaymentRejectedConsumer(
    RabbitMqSettings settings, IServiceScopeFactory scopes, ILogger<PaymentRejectedConsumer> log) : BackgroundService
{
    private const string Exchange = "swiftride.events";
    private const string RoutingKey = "trip.payment-rejected";
    private const string Queue = "payment.trip-rejections";
    private const string DeadLetterExchange = "swiftride.payment.compensation.dlx";   // riêng, không lẫn với DLQ của ride.completed
    private const string DeadLetterQueue = "payment.trip-rejections.dlq";
    private const int MaxAttempts = 3;

    // Hợp đồng sự kiện: bản riêng của Payment, không dùng chung code với Trip
    private sealed record RejectedMessage(Guid EventId, Guid PaymentId, Guid TripId, string? Reason, DateTime OccurredAt);

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
            var msg = JsonSerializer.Deserialize<RejectedMessage>(ea.Body.Span, JsonSerializerOptions.Web)
                      ?? throw new JsonException("Message rỗng");

            await ProcessWithRetryAsync(msg, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            log.LogError(ex, "Không bồi thường được, chuyển vào DLQ để người xử lý");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    private async Task ProcessWithRetryAsync(RejectedMessage msg, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IHandler<CompensatePaymentCommand, PaymentDto>>();

                var result = await handler.HandleAsync(
                    new CompensatePaymentCommand(msg.PaymentId, msg.TripId, msg.Reason ?? "Trip từ chối kết quả thanh toán"), ct);

                log.LogInformation("Bồi thường chuyến {TripId}: thanh toán {PaymentId} -> {Status}",
                    msg.TripId, result.Id, result.Status);
                return;
            }
            // Dữ liệu bất thường: thử lại vô ích, vào DLQ ngay
            catch (Exception ex) when (ex is NotFoundException or DomainException) { throw; }
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested)   // xmin xung đột, cổng không phản hồi
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                log.LogWarning(ex, "Lần {Attempt}/{Max} thất bại, thử lại sau {Delay}", attempt, MaxAttempts, delay);
                await Task.Delay(delay, ct);
            }
        }
    }
}