using MatchDay.Messaging;
using MatchDay.Messaging.Contracts;

namespace MatchDay.Notifications.Worker.Tickets;

/// <summary>
/// An e-ticket that has been sent to a customer. Exactly one exists per booking, which is what makes
/// handling a redelivered <see cref="BookingConfirmed"/> message safe.
/// </summary>
public sealed class ETicket
{
    public Guid Id { get; init; }

    public Guid BookingId { get; init; }

    public required string TicketCode { get; init; }

    public required string CustomerEmail { get; init; }

    public required string MatchTitle { get; init; }

    public DateTimeOffset KickOff { get; init; }

    public required string Stadium { get; init; }

    public required string Section { get; init; }

    public required List<string> Seats { get; init; }

    public decimal TotalPrice { get; init; }

    public required string Currency { get; init; }

    /// <summary>The message that triggered the e-ticket, for troubleshooting duplicates.</summary>
    public required string MessageId { get; init; }

    /// <summary>The distributed trace the booking was made in - paste it into the dashboard to see the whole flow.</summary>
    public string? TraceId { get; init; }

    public DateTimeOffset SentAt { get; init; }

    public static ETicket Issue(BookingConfirmed booking, MessageContext context, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        BookingId = booking.BookingId,
        // The tail of a version 7 GUID is random, unlike its time-ordered head.
        TicketCode = $"MD-{booking.BookingId.ToString("N")[^10..].ToUpperInvariant()}",
        CustomerEmail = booking.CustomerEmail,
        MatchTitle = booking.MatchTitle,
        KickOff = booking.KickOff,
        Stadium = booking.Stadium,
        Section = booking.Section,
        Seats = [.. booking.Seats],
        TotalPrice = booking.TotalPrice,
        Currency = booking.Currency,
        MessageId = context.MessageId,
        TraceId = context.TraceId,
        SentAt = now,
    };
}
