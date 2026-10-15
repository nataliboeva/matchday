namespace MatchDay.Catalog.Api.Matches;

public sealed record MatchSummary(
    int Id,
    string HomeTeam,
    string AwayTeam,
    string Competition,
    DateTimeOffset KickOff,
    string Stadium,
    string City,
    decimal PriceFrom,
    string Currency);

public sealed record MatchDetails(
    int Id,
    string HomeTeam,
    string AwayTeam,
    string Competition,
    DateTimeOffset KickOff,
    string Stadium,
    string City,
    string Currency,
    IReadOnlyList<SectionSummary> Sections);

public sealed record SectionSummary(int Id, string Name, int Rows, int SeatsPerRow, decimal Price);

/// <summary>
/// Everything the booking service needs to price and validate seats in one section of one match.
/// </summary>
public sealed record SectionOffer(
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    int SectionId,
    string SectionName,
    int Rows,
    int SeatsPerRow,
    decimal Price,
    string Currency);
