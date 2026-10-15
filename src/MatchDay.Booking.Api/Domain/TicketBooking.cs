namespace MatchDay.Booking.Api.Domain;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
}

/// <summary>
/// A confirmed purchase of one or more seats. Created from a <see cref="SeatHold"/> and stored in PostgreSQL,
/// which is the source of truth for sold seats.
/// </summary>
public sealed class TicketBooking
{
    private readonly List<BookedSeat> _seats = [];

    private TicketBooking()
    {
        // Used by EF Core
    }

    public Guid Id { get; private set; }

    public Guid HoldId { get; private set; }

    /// <summary>Client-supplied key that makes confirming the same hold safe to retry.</summary>
    public string IdempotencyKey { get; private set; } = null!;

    public MatchSection Section { get; private set; } = null!;

    public string CustomerEmail { get; private set; } = null!;

    public PriceBreakdown Price { get; private set; } = null!;

    public BookingStatus Status { get; private set; }

    public DateTimeOffset ConfirmedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyList<BookedSeat> Seats => _seats;

    public static TicketBooking Confirm(SeatHold hold, string idempotencyKey, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hold);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        if (hold.IsExpired(now))
        {
            throw new DomainException($"The hold expired at {hold.ExpiresAt:u}. Please select your seats again.");
        }

        var booking = new TicketBooking
        {
            Id = Guid.CreateVersion7(now),
            HoldId = hold.Id,
            IdempotencyKey = idempotencyKey,
            Section = hold.Section,
            CustomerEmail = hold.CustomerEmail,
            Price = hold.Price,
            Status = BookingStatus.Confirmed,
            ConfirmedAt = now,
        };

        booking._seats.AddRange(hold.Seats.Select(seat => new BookedSeat(hold.Section, seat)));

        return booking;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new DomainException("The booking is already cancelled.");
        }

        if (now >= Section.KickOff)
        {
            throw new DomainException("Bookings can't be cancelled after kick-off.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = now;

        foreach (var seat in _seats)
        {
            seat.Release();
        }
    }
}

/// <summary>
/// A seat sold as part of a booking. Match and section are repeated here so the database can enforce
/// "each seat is sold at most once" with a unique index that ignores released seats.
/// </summary>
public sealed class BookedSeat
{
    private BookedSeat()
    {
        // Used by EF Core
    }

    internal BookedSeat(MatchSection section, Seat seat)
    {
        MatchId = section.MatchId;
        SectionId = section.SectionId;
        Row = seat.Row;
        Number = seat.Number;
    }

    public int MatchId { get; private set; }

    public int SectionId { get; private set; }

    public int Row { get; private set; }

    public int Number { get; private set; }

    public bool IsReleased { get; private set; }

    public Seat ToSeat() => new(Row, Number);

    internal void Release() => IsReleased = true;
}
