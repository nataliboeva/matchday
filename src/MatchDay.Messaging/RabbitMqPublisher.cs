using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MatchDay.Messaging;

internal sealed partial class RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    : IMessagePublisher, IAsyncDisposable
{
    // A channel must not be used concurrently, so publishes are serialized over one long-lived channel.
    // That's plenty for this workload; a channel pool would be the next step if it ever isn't.
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var routingKey = MessageTopology.RoutingKeyFor<TMessage>();
        using var activity = MessagingTelemetry.StartPublishActivity(routingKey);

        var properties = new BasicProperties
        {
            MessageId = Guid.NewGuid().ToString(),
            Type = routingKey,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>(),
        };
        MessagingTelemetry.InjectTraceContext(activity ?? Activity.Current, properties.Headers);
        activity?.SetTag("messaging.message.id", properties.MessageId);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, MessageTopology.SerializerOptions);

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            // With publisher confirms enabled this only completes once the broker has accepted the message.
            await channel.BasicPublishAsync(
                MessageTopology.EventsExchange, routingKey, mandatory: true, properties, body, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            _channelLock.Release();
        }

        LogPublished(routingKey, properties.MessageId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        _channelLock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        _channel?.Dispose();

        var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
        _channel = await connection.CreateChannelAsync(options, cancellationToken);
        await MessageTopology.DeclareEventsExchangeAsync(_channel, cancellationToken);

        return _channel;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {MessageType} message {MessageId}")]
    private partial void LogPublished(string messageType, string? messageId);
}
