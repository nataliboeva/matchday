using MatchDay.Booking.Api.Catalog;
using MatchDay.Booking.Api.Data;
using MatchDay.Booking.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MatchDay.Booking.Api.Holds;

public static class HoldEndpoints
{
    public static IEndpointRouteBuilder MapHoldEndpoints(this IEndpointRouteBuilder app)
    {
        var holds = app.MapGroup("/holds").WithTags("Seat holds");

        holds.MapPost("/", HoldSeats)
            .WithName("HoldSeats")
            .WithSummary("Holds up to 6 seats for 10 minutes while the customer checks out.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        holds.MapGet("/{holdId:guid}", GetHold)
            .WithName("GetHold")
            .WithSummary("Gets an active hold. Expired holds are gone.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        holds.MapDelete("/{holdId:guid}", ReleaseHold)
            .WithName("ReleaseHold")
            .WithSummary("Releases a hold early so the seats become available again.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Created<HoldResponse>, ValidationProblem, ProblemHttpResult>> HoldSeats(
        HoldSeatsRequest request,
        CatalogClient catalog,
        BookingDbContext db,
        ISeatHoldStore holdStore,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var seats = request.Seats.Select(s => new Seat(s.Row, s.Number)).ToList();
        if (seats.Distinct().Count() != seats.Count)
        {
            return SeatsValidationProblem("Each seat can only be requested once.");
        }

        var offer = await catalog.GetSectionOfferAsync(request.MatchId, request.SectionId, cancellationToken);
        if (offer is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match or section not found.",
                detail: $"There is no section {request.SectionId} on sale for match {request.MatchId}.");
        }

        var unknownSeats = seats.Where(seat => !offer.HasSeat(seat)).ToList();
        if (unknownSeats.Count > 0)
        {
            return SeatsValidationProblem($"{offer.SectionName} has no {string.Join("; ", unknownSeats)}.");
        }

        var soldSeats = await db.FindSoldSeatsAsync(offer.MatchId, offer.SectionId, seats, cancellationToken);
        if (soldSeats.Count > 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Seats are already sold.",
                detail: $"Already sold: {string.Join("; ", soldSeats)}. Please pick different seats.");
        }

        var hold = SeatHold.Create(
            offer.ToMatchSection(), seats, request.CustomerEmail, timeProvider.GetUtcNow(), SeatHold.DefaultDuration);

        if (!await holdStore.TryCreateAsync(hold, cancellationToken))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Seats are no longer available.",
                detail: "One or more of the selected seats are being booked by someone else. Please pick different seats.");
        }

        return TypedResults.Created($"/holds/{hold.Id}", HoldResponse.From(hold));
    }

    private static async Task<Results<Ok<HoldResponse>, NotFound>> GetHold(
        Guid holdId, ISeatHoldStore holdStore, CancellationToken cancellationToken)
    {
        var hold = await holdStore.GetAsync(holdId, cancellationToken);

        return hold is null ? TypedResults.NotFound() : TypedResults.Ok(HoldResponse.From(hold));
    }

    private static async Task<Results<NoContent, NotFound>> ReleaseHold(
        Guid holdId, ISeatHoldStore holdStore, CancellationToken cancellationToken)
    {
        var hold = await holdStore.GetAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return TypedResults.NotFound();
        }

        await holdStore.ReleaseAsync(hold, cancellationToken);
        return TypedResults.NoContent();
    }

    private static ValidationProblem SeatsValidationProblem(string error) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(HoldSeatsRequest.Seats)] = [error] });
}
