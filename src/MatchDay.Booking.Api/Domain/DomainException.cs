namespace MatchDay.Booking.Api.Domain;

/// <summary>
/// Raised when an operation breaks a booking rule, e.g. confirming an expired hold.
/// The API translates it into a 409 Conflict problem response.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
