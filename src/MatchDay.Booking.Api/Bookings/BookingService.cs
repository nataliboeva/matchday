using MatchDay.Booking.Api.Data;
using MatchDay.Booking.Api.Domain;
using MatchDay.Booking.Api.Holds;
using MatchDay.Messaging;
using Microsoft.EntityFrameworkCore;

namespace MatchDay.Booking.Api.Bookings;

public enum ConfirmOutcome
{
    Created,
    Replayed,
    HoldNotFound,
    IdempotencyKeyReused,
    HoldAlreadyConfirmed,
    SeatsAlreadySold,
}

public sealed record ConfirmResult(ConfirmOutcome Outcome, TicketBooking? Booking = null);

/// <summary>
/// Turns a seat hold into a stored booking. Safe to retry: the same idempotency key always returns
/// the booking created by the first successful attempt.
/// </summary>
public sealed partial class BookingService(
    BookingDbContext db,
    ISeatHoldStore holdStore,
    IMessagePublisher publisher,
    BookingMetrics metrics,
    TimeProvider timeProvider,
    ILogger<BookingService> logger)
{
    public async Task<ConfirmResult> ConfirmAsync(Guid holdId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var previous = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (previous is not null)
        {
            return Replay(previous, holdId);
        }

        var hold = await holdStore.GetAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return new ConfirmResult(ConfirmOutcome.HoldNotFound);
        }

        var booking = TicketBooking.Confirm(hold, idempotencyKey, timeProvider.GetUtcNow());
        db.Bookings.Add(booking);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.IsUniqueViolationOf(BookingDbContext.IdempotencyKeyIndex) || ex.IsUniqueViolationOf(BookingDbContext.HoldIdIndex))
        {
            // Either a concurrent retry with the same key won the race (answer with its booking),
            // or this hold was already confirmed under a different key.
            var winner = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            return winner is not null
                ? Replay(winner, holdId)
                : new ConfirmResult(ConfirmOutcome.HoldAlreadyConfirmed);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolationOf(BookingDbContext.SoldSeatIndex))
        {
            return new ConfirmResult(ConfirmOutcome.SeatsAlreadySold);
        }

        LogBookingConfirmed(booking.Id, booking.Seats.Count, booking.Section.MatchTitle);
        metrics.RecordTicketsSold(booking);

        // The seats are now sold in PostgreSQL, so the Redis locks are no longer needed.
        await holdStore.ReleaseAsync(hold, cancellationToken);
        await PublishConfirmationAsync(booking, cancellationToken);

        return new ConfirmResult(ConfirmOutcome.Created, booking);
    }

    private async Task PublishConfirmationAsync(TicketBooking booking, CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(booking.ToBookingConfirmed(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The booking is already stored, so the customer keeps their seats; only the e-ticket is delayed.
            // A transactional outbox would make this reliable - see the roadmap in the README.
            LogPublishFailed(ex, booking.Id);
        }
    }

    private Task<TicketBooking?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        db.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.IdempotencyKey == idempotencyKey, cancellationToken);

    private static ConfirmResult Replay(TicketBooking previous, Guid holdId) =>
        previous.HoldId == holdId
            ? new ConfirmResult(ConfirmOutcome.Replayed, previous)
            : new ConfirmResult(ConfirmOutcome.IdempotencyKeyReused);

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} confirmed: {SeatCount} seat(s) for {Match}")]
    private partial void LogBookingConfirmed(Guid bookingId, int seatCount, string match);

    [LoggerMessage(Level = LogLevel.Error, Message = "Booking {BookingId} was stored but its BookingConfirmed event could not be published")]
    private partial void LogPublishFailed(Exception exception, Guid bookingId);
}
