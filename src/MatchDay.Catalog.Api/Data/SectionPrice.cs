namespace MatchDay.Catalog.Api.Data;

/// <summary>
/// The ticket price for one section at one match. Prices differ per match, e.g. a derby costs more.
/// </summary>
public sealed class SectionPrice
{
    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public int SectionId { get; set; }

    public Section Section { get; set; } = null!;

    public decimal Amount { get; set; }
}
