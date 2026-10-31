using RabbitMQ.Client;

namespace TripService.Infrastructure.Messaging;

public interface IRawPublisher
{
    Task PublishAsync(string routingKey, string type, byte[] body, string? traceParent, CancellationToken ct = default);
}

public sealed class RabbitMqPublisher(string uri) : IRawPublisher, IAsyncDisposable
{
    public const string Exchange = "swiftride.events";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task PublishAsync(string routingKey, string type, byte[] body, string? traceParent, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var connection = await GetConnectionAsync(cts.Token);

            // Publisher confirms: chỉ xong khi broker xác nhận đã nhận, không thì ném lỗi
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cts.Token);

            await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: cts.Token);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = type,
                Headers = traceParent is null
                    ? null
                    : new Dictionary<string, object?> { [MessagingTelemetry.TraceParentHeader] = traceParent }
            };
            await channel.BasicPublishAsync(Exchange, routingKey, mandatory: false, basicProperties: props, body: body,
                cancellationToken: cts.Token);
        }
        catch
        {
            // Kết nối có thể đã hỏng: bỏ để lần sau tạo lại
            if (_connection is { IsOpen: false })
            {
                try { await _connection.DisposeAsync(); } catch { }
                _connection = null;
            }
            throw;
        }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            if (_connection is not null) await _connection.DisposeAsync();

            var factory = new ConnectionFactory
            {
                Uri = new Uri(uri),
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };
            _connection = await factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally { _lock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _lock.Dispose();
    }
}