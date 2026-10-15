namespace MatchDay.Catalog.Api.Data;

public sealed class Stadium
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string City { get; set; }

    public List<Section> Sections { get; set; } = [];
}
