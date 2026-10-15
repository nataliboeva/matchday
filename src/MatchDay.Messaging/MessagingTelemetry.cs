using System.Diagnostics;
using System.Text;

namespace MatchDay.Messaging;

/// <summary>
/// Tracing for publish/consume, plus W3C trace context propagation through message headers.
/// Uses the same <see cref="DistributedContextPropagator"/> that ASP.NET Core and HttpClient use,
/// so a trace started by an HTTP request continues seamlessly across RabbitMQ.
/// </summary>
internal static class MessagingTelemetry
{
    public const string ActivitySourceName = "MatchDay.Messaging";

    private static readonly ActivitySource Source = new(ActivitySourceName);

    public static Activity? StartPublishActivity(string routingKey) =>
        Source.StartActivity($"publish {routingKey}", ActivityKind.Producer)?
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.operation.type", "send")
            .SetTag("messaging.destination.name", MessageTopology.EventsExchange)
            .SetTag("messaging.rabbitmq.destination.routing_key", routingKey);

    public static Activity? StartProcessActivity(string queueName, string routingKey, ActivityContext parentContext) =>
        Source.StartActivity($"process {routingKey}", ActivityKind.Consumer, parentContext)?
            .SetTag("messaging.system", "rabbitmq")
            .SetTag("messaging.operation.type", "process")
            .SetTag("messaging.destination.name", queueName)
            .SetTag("messaging.rabbitmq.destination.routing_key", routingKey);

    public static void InjectTraceContext(Activity? activity, IDictionary<string, object?> headers) =>
        DistributedContextPropagator.Current.Inject(activity, headers, static (carrier, name, value) =>
        {
            if (carrier is IDictionary<string, object?> target)
            {
                target[name] = value;
            }
        });

    public static ActivityContext ExtractTraceContext(IDictionary<string, object?>? headers)
    {
        if (headers is null)
        {
            return default;
        }

        DistributedContextPropagator.Current.ExtractTraceIdAndState(
            headers,
            static (object? carrier, string name, out string? value, out IEnumerable<string>? values) =>
            {
                values = null;
                value = carrier is IDictionary<string, object?> source && source.TryGetValue(name, out var raw)
                    ? raw switch
                    {
                        // AMQP delivers string headers as raw bytes
                        byte[] bytes => Encoding.UTF8.GetString(bytes),
                        string text => text,
                        _ => null,
                    }
                    : null;
            },
            out var traceParent,
            out var traceState);

        return ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var context) ? context : default;
    }
}
