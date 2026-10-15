using MatchDay.Messaging;
using MatchDay.Messaging.Contracts;
using MatchDay.Notifications.Worker.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchDay.Notifications.Worker.Tickets;

/// <summary>
/// Issues the e-ticket for a confirmed booking. RabbitMQ delivers at least once, so the handler
/// checks whether the booking already has a ticket and quietly skips duplicates.
/// </summary>
public sealed partial class BookingConfirmedHandler(
    NotificationsDbContext db,
    ITicketSender ticketSender,
    TimeProvider timeProvider,
    ILogger<BookingConfirmedHandler> logger) : IMessageHandler<BookingConfirmed>
{
    public async Task HandleAsync(BookingConfirmed message, MessageContext context, CancellationToken cancellationToken)
    {
        if (await db.Tickets.AnyAsync(t => t.BookingId == message.BookingId, cancellationToken))
        {
            LogDuplicate(message.BookingId, context.MessageId);
            return;
        }

        var ticket = ETicket.Issue(message, context, timeProvider.GetUtcNow());

        // Send first, then record. If the process dies in between, the redelivered message sends the
        // e-ticket again: a duplicate e-mail is better than a customer without a ticket.
        await ticketSender.SendAsync(ticket, cancellationToken);

        db.Tickets.Add(ticket);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent delivery of the same booking may have recorded its ticket first (unique index on BookingId).
            if (!await db.Tickets.AsNoTracking().AnyAsync(t => t.BookingId == message.BookingId, cancellationToken))
            {
                throw;
            }

            LogDuplicate(message.BookingId, context.MessageId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-ticket for booking {BookingId} was already sent; ignoring duplicate message {MessageId}")]
    private partial void LogDuplicate(Guid bookingId, string messageId);
}
