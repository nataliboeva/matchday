using MatchDay.Booking.Api.Domain;
using MatchDay.Booking.Api.Holds;

namespace MatchDay.Booking.Api.Bookings;

public sealed record ConfirmBookingRequest(Guid HoldId);

public sealed record BookingResponse(
    Guid Id,
    Guid HoldId,
    BookingStatus Status,
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    string Section,
    IReadOnlyList<SeatDto> Seats,
    string CustomerEmail,
    PriceBreakdown Price,
    string Currency,
    DateTimeOffset ConfirmedAt,
    DateTimeOffset? CancelledAt)
{
    public static BookingResponse From(TicketBooking booking) => new(
        booking.Id,
        booking.HoldId,
        booking.Status,
        booking.Section.MatchId,
        booking.Section.MatchTitle,
        booking.Section.KickOff,
        booking.Section.Stadium,
        booking.Section.SectionName,
        [.. booking.Seats.Select(s => new SeatDto(s.Row, s.Number))],
        booking.CustomerEmail,
        booking.Price,
        TicketPricing.Currency,
        booking.ConfirmedAt,
        booking.CancelledAt);
}
