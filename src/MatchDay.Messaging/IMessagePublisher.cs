namespace MatchDay.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the MatchDay event exchange and waits for the broker to confirm it.
    /// The current trace context travels with the message, so consumers continue the same trace.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
