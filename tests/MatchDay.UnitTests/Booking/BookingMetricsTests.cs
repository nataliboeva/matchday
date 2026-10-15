using System.Diagnostics.Metrics;
using MatchDay.Booking.Api.Bookings;
using MatchDay.Booking.Api.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace MatchDay.UnitTests.Booking;

public class BookingMetricsTests
{
    [Fact]
    public void Tickets_sold_counts_one_ticket_per_seat_tagged_by_match_and_section()
    {
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var meterFactory = services.GetRequiredService<IMeterFactory>();
        using var collector = new MetricCollector<int>(meterFactory, BookingMetrics.MeterName, BookingMetrics.TicketsSoldInstrument);
        var metrics = new BookingMetrics(meterFactory);
        var booking = TicketBooking.Confirm(TestData.Hold(new Seat(1, 1), new Seat(1, 2), new Seat(1, 3)), "key-1", TestData.Now);

        metrics.RecordTicketsSold(booking);

        var measurement = collector.GetMeasurementSnapshot().ShouldHaveSingleItem();
        measurement.Value.ShouldBe(3);
        measurement.Tags["matchday.match"].ShouldBe("Trimontium Plovdiv vs Vitosha Sofia");
        measurement.Tags["matchday.section"].ShouldBe("Main Tribune");
    }
}
