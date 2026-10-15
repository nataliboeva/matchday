namespace MatchDay.Messaging;

/// <summary>
/// Handles one message type. Handlers are resolved from a fresh DI scope per message and must be
/// idempotent: the broker delivers at least once, so the same message can arrive more than once.
/// </summary>
public interface IMessageHandler<in TMessage>
    where TMessage : class
{
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken cancellationToken);
}

/// <param name="MessageId">Unique id assigned by the publisher.</param>
/// <param name="Redelivered">True when the broker has tried to deliver this message before.</param>
/// <param name="TraceId">W3C trace id the message was published under, if any.</param>
public sealed record MessageContext(string MessageId, bool Redelivered, string? TraceId);
