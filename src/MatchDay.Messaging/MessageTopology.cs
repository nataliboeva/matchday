using System.Text.Json;
using RabbitMQ.Client;

namespace MatchDay.Messaging;

/// <summary>
/// One durable topic exchange for all integration events, routed by message type name.
/// Every consumer queue gets a dead-letter queue, so messages that keep failing are parked, not lost.
/// </summary>
internal static class MessageTopology
{
    public const string EventsExchange = "matchday.events";
    public const string DeadLetterExchange = "matchday.dead-letter";

    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string RoutingKeyFor<TMessage>() => typeof(TMessage).Name;

    public static Task DeclareEventsExchangeAsync(IChannel channel, CancellationToken cancellationToken) =>
        channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

    public static async Task DeclareConsumerQueueAsync(
        IChannel channel, string queueName, string routingKey, CancellationToken cancellationToken)
    {
        await DeclareEventsExchangeAsync(channel, cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

        var deadLetterQueue = $"{queueName}.dead-letter";
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(deadLetterQueue, DeadLetterExchange, routingKey: queueName, cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = queueName,
        };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, EventsExchange, routingKey, cancellationToken: cancellationToken);
    }
}
