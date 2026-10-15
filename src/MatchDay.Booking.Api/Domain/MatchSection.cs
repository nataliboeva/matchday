namespace MatchDay.Booking.Api.Domain;

/// <summary>
/// The section of a match the customer is buying into, captured from the catalog when seats are held.
/// Keeping a snapshot locks the price for the lifetime of the hold and gives the e-ticket everything it needs.
/// </summary>
public sealed record MatchSection(
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    int SectionId,
    string SectionName,
    decimal UnitPrice);
