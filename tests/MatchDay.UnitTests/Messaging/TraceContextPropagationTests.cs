using System.Diagnostics;
using System.Text;
using MatchDay.Messaging;

namespace MatchDay.UnitTests.Messaging;

public class TraceContextPropagationTests
{
    [Fact]
    public void Injected_trace_context_can_be_extracted_from_amqp_headers()
    {
        using var activity = new Activity("publish").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, object?>();

        MessagingTelemetry.InjectTraceContext(activity, headers);

        // RabbitMQ hands string headers back to consumers as UTF-8 bytes
        var delivered = headers.ToDictionary(h => h.Key, h => (object?)Encoding.UTF8.GetBytes((string)h.Value!));
        var extracted = MessagingTelemetry.ExtractTraceContext(delivered);

        extracted.TraceId.ShouldBe(activity.TraceId);
        extracted.SpanId.ShouldBe(activity.SpanId);
        extracted.IsRemote.ShouldBeTrue();
    }

    [Fact]
    public void Extracting_from_a_w3c_traceparent_header_restores_the_parent()
    {
        var headers = new Dictionary<string, object?>
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
        };

        var extracted = MessagingTelemetry.ExtractTraceContext(headers);

        extracted.TraceId.ToHexString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        extracted.SpanId.ToHexString().ShouldBe("b7ad6b7169203331");
        extracted.TraceFlags.ShouldBe(ActivityTraceFlags.Recorded);
    }

    [Fact]
    public void Messages_without_trace_headers_start_a_new_trace()
    {
        MessagingTelemetry.ExtractTraceContext(new Dictionary<string, object?>()).ShouldBe(default);
        MessagingTelemetry.ExtractTraceContext(null).ShouldBe(default);
    }

    [Fact]
    public void Routing_key_is_the_message_type_name()
    {
        MessageTopology.RoutingKeyFor<MatchDay.Messaging.Contracts.BookingConfirmed>().ShouldBe("BookingConfirmed");
    }
}
