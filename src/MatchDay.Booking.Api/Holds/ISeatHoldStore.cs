using MatchDay.Booking.Api.Domain;

namespace MatchDay.Booking.Api.Holds;

public interface ISeatHoldStore
{
    /// <summary>
    /// Stores the hold and locks all of its seats until the hold expires.
    /// All-or-nothing: returns false without locking anything if any seat is already held.
    /// </summary>
    Task<bool> TryCreateAsync(SeatHold hold, CancellationToken cancellationToken);

    /// <summary>Returns the hold, or null if it doesn't exist or has expired.</summary>
    Task<SeatHold?> GetAsync(Guid holdId, CancellationToken cancellationToken);

    /// <summary>Removes the hold and unlocks the seats that still belong to it.</summary>
    Task ReleaseAsync(SeatHold hold, CancellationToken cancellationToken);
}
