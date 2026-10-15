namespace MatchDay.Catalog.Api.Data;

/// <summary>
/// A block of seats in a stadium. Seats are addressed by row (1..Rows) and number (1..SeatsPerRow).
/// </summary>
public sealed class Section
{
    public int Id { get; set; }

    public int StadiumId { get; set; }

    public required string Name { get; set; }

    public int Rows { get; set; }

    public int SeatsPerRow { get; set; }
}
