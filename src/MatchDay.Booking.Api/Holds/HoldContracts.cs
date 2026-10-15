using System.ComponentModel.DataAnnotations;
using MatchDay.Booking.Api.Domain;

namespace MatchDay.Booking.Api.Holds;

public sealed record SeatDto([Range(1, 500)] int Row, [Range(1, 500)] int Number);

public sealed record HoldSeatsRequest(
    [Range(1, int.MaxValue)] int MatchId,
    [Range(1, int.MaxValue)] int SectionId,
    [Required, EmailAddress, MaxLength(254)] string CustomerEmail,
    [Required, Length(1, SeatHold.MaxSeats)] List<SeatDto> Seats);

public sealed record HoldResponse(
    Guid HoldId,
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    string Section,
    IReadOnlyList<SeatDto> Seats,
    PriceBreakdown Price,
    string Currency,
    DateTimeOffset ExpiresAt)
{
    public static HoldResponse From(SeatHold hold) => new(
        hold.Id,
        hold.Section.MatchId,
        hold.Section.MatchTitle,
        hold.Section.KickOff,
        hold.Section.Stadium,
        hold.Section.SectionName,
        [.. hold.Seats.Select(s => new SeatDto(s.Row, s.Number))],
        hold.Price,
        TicketPricing.Currency,
        hold.ExpiresAt);
}
