using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MatchDay.Messaging;

/// <summary>
/// Consumes one message type from its own durable queue and dispatches each message to the registered
/// <see cref="IMessageHandler{TMessage}"/>. A failing message is retried once, then dead-lettered.
/// </summary>
internal sealed partial class RabbitMqConsumer<TMessage>(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    string queueName,
    ILogger<RabbitMqConsumer<TMessage>> logger) : BackgroundService
    where TMessage : class
{
    private const ushort PrefetchCount = 10;

    private readonly string _routingKey = MessageTopology.RoutingKeyFor<TMessage>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await MessageTopology.DeclareConsumerQueueAsync(channel, queueName, _routingKey, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
        LogConsuming(queueName, _routingKey);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var parentContext = MessagingTelemetry.ExtractTraceContext(delivery.BasicProperties.Headers);
        using var activity = MessagingTelemetry.StartProcessActivity(queueName, _routingKey, parentContext);

        var messageId = delivery.BasicProperties.MessageId ?? string.Empty;
        activity?.SetTag("messaging.message.id", messageId);

        try
        {
            var message = JsonSerializer.Deserialize<TMessage>(delivery.Body.Span, MessageTopology.SerializerOptions)
                ?? throw new JsonException($"Message {messageId} has an empty body.");

            var traceId = parentContext == default ? null : parentContext.TraceId.ToHexString();
            var context = new MessageContext(messageId, delivery.Redelivered, traceId);

            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
            await handler.HandleAsync(message, context, stoppingToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);

            // Retry once in case the failure was transient; a second failure goes to the dead-letter queue.
            var requeue = !delivery.Redelivered;
            LogHandlingFailed(ex, _routingKey, messageId, requeue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue, CancellationToken.None);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming {MessageType} messages from queue {QueueName}")]
    private partial void LogConsuming(string queueName, string messageType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle {MessageType} message {MessageId} (requeue: {Requeue})")]
    private partial void LogHandlingFailed(Exception exception, string messageType, string messageId, bool requeue);
}
