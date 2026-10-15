using MatchDay.Booking.Api.Domain;
using MatchDay.Messaging.Contracts;

namespace MatchDay.Booking.Api.Bookings;

internal static class BookingEvents
{
    public static BookingConfirmed ToBookingConfirmed(this TicketBooking booking) => new(
        booking.Id,
        booking.CustomerEmail,
        booking.Section.MatchId,
        booking.Section.MatchTitle,
        booking.Section.KickOff,
        booking.Section.Stadium,
        booking.Section.SectionName,
        [.. booking.Seats.Select(seat => seat.ToSeat().ToString())],
        booking.Price.Total,
        TicketPricing.Currency,
        booking.ConfirmedAt);
}
