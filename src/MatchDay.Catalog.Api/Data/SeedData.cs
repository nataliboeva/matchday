using Microsoft.EntityFrameworkCore;

namespace MatchDay.Catalog.Api.Data;

/// <summary>
/// Fictional clubs, stadiums and 2026/27 fixtures. Seeded through migrations (HasData),
/// so every environment starts with the same catalog. All kick-off times are UTC.
/// </summary>
internal static class SeedData
{
    private const string Competition = "First League";

    private static readonly Stadium[] Stadiums =
    [
        new() { Id = 1, Name = "Trimontium Arena", City = "Plovdiv" },
        new() { Id = 2, Name = "Vitosha Park", City = "Sofia" },
        new() { Id = 3, Name = "Seafront Stadium", City = "Varna" },
    ];

    // Base prices in EUR; the match "demand" multiplier is applied on top.
    private static readonly (Section Section, decimal BasePrice)[] Sections =
    [
        (new() { Id = 1, StadiumId = 1, Name = "North Stand", Rows = 18, SeatsPerRow = 30 }, 14m),
        (new() { Id = 2, StadiumId = 1, Name = "South Stand", Rows = 18, SeatsPerRow = 30 }, 14m),
        (new() { Id = 3, StadiumId = 1, Name = "Main Tribune", Rows = 12, SeatsPerRow = 40 }, 24m),
        (new() { Id = 4, StadiumId = 1, Name = "VIP Lounge", Rows = 4, SeatsPerRow = 20 }, 55m),

        (new() { Id = 5, StadiumId = 2, Name = "North Stand", Rows = 20, SeatsPerRow = 32 }, 15m),
        (new() { Id = 6, StadiumId = 2, Name = "South Stand", Rows = 20, SeatsPerRow = 32 }, 15m),
        (new() { Id = 7, StadiumId = 2, Name = "Main Tribune", Rows = 14, SeatsPerRow = 40 }, 26m),
        (new() { Id = 8, StadiumId = 2, Name = "VIP Lounge", Rows = 5, SeatsPerRow = 20 }, 60m),

        (new() { Id = 9, StadiumId = 3, Name = "North Stand", Rows = 16, SeatsPerRow = 28 }, 12m),
        (new() { Id = 10, StadiumId = 3, Name = "South Stand", Rows = 16, SeatsPerRow = 28 }, 12m),
        (new() { Id = 11, StadiumId = 3, Name = "Main Tribune", Rows = 12, SeatsPerRow = 36 }, 22m),
        (new() { Id = 12, StadiumId = 3, Name = "VIP Lounge", Rows = 4, SeatsPerRow = 16 }, 50m),
    ];

    private static readonly (Match Match, decimal Demand)[] Matches =
    [
        (Fixture(1, "Trimontium Plovdiv", "Vitosha Sofia", stadiumId: 1, new(2026, 10, 31, 17, 30, 0, TimeSpan.Zero)), 1.5m),
        (Fixture(2, "Varna Mariners", "Danube Ruse", stadiumId: 3, new(2026, 11, 7, 13, 0, 0, TimeSpan.Zero)), 1.0m),
        (Fixture(3, "Vitosha Sofia", "Rhodope Eagles", stadiumId: 2, new(2026, 11, 21, 15, 15, 0, TimeSpan.Zero)), 1.0m),
        (Fixture(4, "Trimontium Plovdiv", "Varna Mariners", stadiumId: 1, new(2026, 11, 28, 15, 30, 0, TimeSpan.Zero)), 1.2m),
        (Fixture(5, "Vitosha Sofia", "Trimontium Plovdiv", stadiumId: 2, new(2027, 3, 6, 16, 0, 0, TimeSpan.Zero)), 1.5m),
        (Fixture(6, "Varna Mariners", "Vitosha Sofia", stadiumId: 3, new(2027, 4, 17, 15, 0, 0, TimeSpan.Zero)), 1.2m),
        (Fixture(7, "Trimontium Plovdiv", "Danube Ruse", stadiumId: 1, new(2027, 5, 22, 16, 30, 0, TimeSpan.Zero)), 1.0m),
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stadium>().HasData(Stadiums);
        modelBuilder.Entity<Section>().HasData(Sections.Select(s => s.Section));
        modelBuilder.Entity<Match>().HasData(Matches.Select(m => m.Match));
        modelBuilder.Entity<SectionPrice>().HasData(Prices());
    }

    private static IEnumerable<SectionPrice> Prices() =>
        from match in Matches
        from section in Sections
        where section.Section.StadiumId == match.Match.StadiumId
        select new SectionPrice
        {
            MatchId = match.Match.Id,
            SectionId = section.Section.Id,
            Amount = Math.Round(section.BasePrice * match.Demand, MidpointRounding.AwayFromZero),
        };

    private static Match Fixture(int id, string home, string away, int stadiumId, DateTimeOffset kickOff) => new()
    {
        Id = id,
        HomeTeam = home,
        AwayTeam = away,
        Competition = Competition,
        StadiumId = stadiumId,
        KickOff = kickOff,
    };
}
