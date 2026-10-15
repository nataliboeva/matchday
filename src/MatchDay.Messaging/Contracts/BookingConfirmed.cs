namespace MatchDay.Messaging.Contracts;

/// <summary>
/// Published by the booking service once a booking is stored. It carries everything needed to issue
/// the e-ticket, so consumers don't have to call back into other services.
/// </summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    string CustomerEmail,
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    string Section,
    IReadOnlyList<string> Seats,
    decimal TotalPrice,
    string Currency,
    DateTimeOffset ConfirmedAt);
