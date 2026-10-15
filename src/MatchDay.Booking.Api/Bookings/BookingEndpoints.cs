using MatchDay.Booking.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchDay.Booking.Api.Bookings;

public static class BookingEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    private const int MaxIdempotencyKeyLength = 100;

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var bookings = app.MapGroup("/bookings").WithTags("Bookings");

        bookings.MapPost("/", ConfirmBooking)
            .WithName("ConfirmBooking")
            .WithSummary("Confirms a seat hold. Requires an Idempotency-Key header; retries with the same key return the original booking.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        bookings.MapGet("/{bookingId:guid}", GetBooking)
            .WithName("GetBooking")
            .WithSummary("Gets a booking.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        bookings.MapPost("/{bookingId:guid}/cancel", CancelBooking)
            .WithName("CancelBooking")
            .WithSummary("Cancels a booking before kick-off and releases its seats.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Created<BookingResponse>, Ok<BookingResponse>, ValidationProblem, ProblemHttpResult>> ConfirmBooking(
        ConfirmBookingRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        BookingService bookingService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [IdempotencyKeyHeader] = [$"Send a unique {IdempotencyKeyHeader} header (up to {MaxIdempotencyKeyLength} characters), e.g. a GUID."],
            });
        }

        var result = await bookingService.ConfirmAsync(request.HoldId, idempotencyKey, cancellationToken);

        return result.Outcome switch
        {
            ConfirmOutcome.Created => TypedResults.Created($"/bookings/{result.Booking!.Id}", BookingResponse.From(result.Booking)),
            ConfirmOutcome.Replayed => TypedResults.Ok(BookingResponse.From(result.Booking!)),
            ConfirmOutcome.HoldNotFound => Problem(
                StatusCodes.Status404NotFound,
                "Hold not found.",
                "The hold doesn't exist or has expired. Please select your seats again."),
            ConfirmOutcome.IdempotencyKeyReused => Problem(
                StatusCodes.Status422UnprocessableEntity,
                "Idempotency key already used.",
                $"This {IdempotencyKeyHeader} was already used to confirm a different hold."),
            ConfirmOutcome.HoldAlreadyConfirmed => Problem(
                StatusCodes.Status409Conflict,
                "Hold already confirmed.",
                "This hold has already been turned into a booking."),
            ConfirmOutcome.SeatsAlreadySold => Problem(
                StatusCodes.Status409Conflict,
                "Seats are no longer available.",
                "One or more of the held seats have been sold in the meantime. Please select your seats again."),
            _ => throw new InvalidOperationException($"Unexpected outcome {result.Outcome}."),
        };
    }

    private static async Task<Results<Ok<BookingResponse>, NotFound>> GetBooking(
        Guid bookingId, BookingDbContext db, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(BookingResponse.From(booking));
    }

    private static async Task<Results<Ok<BookingResponse>, NotFound>> CancelBooking(
        Guid bookingId, BookingDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return TypedResults.NotFound();
        }

        booking.Cancel(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(BookingResponse.From(booking));
    }

    private static ProblemHttpResult Problem(int statusCode, string title, string detail) =>
        TypedResults.Problem(statusCode: statusCode, title: title, detail: detail);
}
