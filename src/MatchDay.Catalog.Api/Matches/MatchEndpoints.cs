using MatchDay.Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MatchDay.Catalog.Api.Matches;

public static class MatchEndpoints
{
    // Bulgaria adopted the euro on 1 January 2026.
    private const string Currency = "EUR";

    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var matches = app.MapGroup("/matches").WithTags("Matches");

        matches.MapGet("/", GetMatches)
            .WithName("GetMatches")
            .WithSummary("Lists all fixtures, ordered by kick-off.");

        matches.MapGet("/{matchId:int}", GetMatch)
            .WithName("GetMatch")
            .WithSummary("Gets a fixture with its sections and ticket prices.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapGet("/{matchId:int}/sections/{sectionId:int}", GetSectionOffer)
            .WithName("GetSectionOffer")
            .WithSummary("Gets the price and seat layout of one section for one fixture.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<List<MatchSummary>>> GetMatches(CatalogDbContext db, CancellationToken cancellationToken)
    {
        var matches = await db.Matches
            .AsNoTracking()
            .OrderBy(m => m.KickOff)
            .Select(m => new MatchSummary(
                m.Id,
                m.HomeTeam,
                m.AwayTeam,
                m.Competition,
                m.KickOff,
                m.Stadium.Name,
                m.Stadium.City,
                m.Prices.Min(p => p.Amount),
                Currency))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(matches);
    }

    private static async Task<Results<Ok<MatchDetails>, NotFound>> GetMatch(
        int matchId, CatalogDbContext db, CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .AsNoTracking()
            .Where(m => m.Id == matchId)
            .Select(m => new MatchDetails(
                m.Id,
                m.HomeTeam,
                m.AwayTeam,
                m.Competition,
                m.KickOff,
                m.Stadium.Name,
                m.Stadium.City,
                Currency,
                m.Prices
                    .OrderBy(p => p.SectionId)
                    .Select(p => new SectionSummary(p.SectionId, p.Section.Name, p.Section.Rows, p.Section.SeatsPerRow, p.Amount))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return match is null ? TypedResults.NotFound() : TypedResults.Ok(match);
    }

    private static async Task<Results<Ok<SectionOffer>, NotFound>> GetSectionOffer(
        int matchId, int sectionId, CatalogDbContext db, CancellationToken cancellationToken)
    {
        var offer = await db.SectionPrices
            .AsNoTracking()
            .Where(p => p.MatchId == matchId && p.SectionId == sectionId)
            .Select(p => new SectionOffer(
                p.MatchId,
                p.Match.HomeTeam + " vs " + p.Match.AwayTeam,
                p.Match.KickOff,
                p.Match.Stadium.Name,
                p.SectionId,
                p.Section.Name,
                p.Section.Rows,
                p.Section.SeatsPerRow,
                p.Amount,
                Currency))
            .SingleOrDefaultAsync(cancellationToken);

        return offer is null ? TypedResults.NotFound() : TypedResults.Ok(offer);
    }
}
