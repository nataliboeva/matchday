namespace MatchDay.Booking.Api.Domain;

/// <summary>
/// Seats reserved for one customer for a short time while they complete the booking.
/// Holds live in Redis and disappear when they expire; confirming one creates a <see cref="TicketBooking"/>.
/// </summary>
public sealed record SeatHold
{
    public const int MaxSeats = 6;

    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(10);

    public required Guid Id { get; init; }

    public required MatchSection Section { get; init; }

    public required IReadOnlyList<Seat> Seats { get; init; }

    public required string CustomerEmail { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public PriceBreakdown Price => TicketPricing.Calculate(Section.UnitPrice, Seats.Count);

    public static SeatHold Create(
        MatchSection section,
        IReadOnlyCollection<Seat> seats,
        string customerEmail,
        DateTimeOffset now,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);

        if (seats.Count is 0 or > MaxSeats)
        {
            throw new DomainException($"A booking must contain between 1 and {MaxSeats} seats.");
        }

        if (seats.Distinct().Count() != seats.Count)
        {
            throw new DomainException("The same seat can't be booked twice.");
        }

        return new SeatHold
        {
            Id = Guid.CreateVersion7(now),
            Section = section,
            Seats = [.. seats.OrderBy(s => s.Row).ThenBy(s => s.Number)],
            CustomerEmail = customerEmail.Trim(),
            CreatedAt = now,
            ExpiresAt = now + duration,
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
