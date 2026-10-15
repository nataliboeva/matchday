using System.Diagnostics.Metrics;
using MatchDay.Booking.Api.Domain;

namespace MatchDay.Booking.Api.Bookings;

/// <summary>
/// Business metrics for the booking service, exported through OpenTelemetry
/// (the service defaults pick up every "MatchDay.*" meter).
/// </summary>
public sealed class BookingMetrics
{
    public const string MeterName = "MatchDay.Booking";
    public const string TicketsSoldInstrument = "matchday.tickets.sold";

    private readonly Counter<int> _ticketsSold;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _ticketsSold = meter.CreateCounter<int>(TicketsSoldInstrument, unit: "{ticket}", description: "Number of tickets sold.");
    }

    public void RecordTicketsSold(TicketBooking booking) =>
        _ticketsSold.Add(
            booking.Seats.Count,
            new KeyValuePair<string, object?>("matchday.match", booking.Section.MatchTitle),
            new KeyValuePair<string, object?>("matchday.section", booking.Section.SectionName));
}
