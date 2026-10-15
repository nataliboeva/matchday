namespace MatchDay.Catalog.Api.Data;

public sealed class Match
{
    public int Id { get; set; }

    public required string HomeTeam { get; set; }

    public required string AwayTeam { get; set; }

    public required string Competition { get; set; }

    /// <summary>Kick-off time in UTC.</summary>
    public DateTimeOffset KickOff { get; set; }

    public int StadiumId { get; set; }

    public Stadium Stadium { get; set; } = null!;

    public List<SectionPrice> Prices { get; set; } = [];
}
