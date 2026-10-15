namespace MatchDay.Notifications.Worker.Tickets;

public interface ITicketSender
{
    Task SendAsync(ETicket ticket, CancellationToken cancellationToken);
}

/// <summary>
/// Stand-in for a real e-mail provider: "sends" the e-ticket by writing a structured log entry,
/// which shows up (with its trace) in the Aspire dashboard.
/// </summary>
public sealed partial class LoggingTicketSender(ILogger<LoggingTicketSender> logger) : ITicketSender
{
    public Task SendAsync(ETicket ticket, CancellationToken cancellationToken)
    {
        LogTicketSent(
            ticket.TicketCode,
            EmailMask.Mask(ticket.CustomerEmail),
            ticket.MatchTitle,
            ticket.KickOff,
            ticket.Section,
            string.Join("; ", ticket.Seats));

        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "E-ticket {TicketCode} sent to {Email}: {Match} at {KickOff:u}, {Section}, {Seats}")]
    private partial void LogTicketSent(string ticketCode, string email, string match, DateTimeOffset kickOff, string section, string seats);
}

public static class EmailMask
{
    /// <summary>Keeps logs useful without writing full e-mail addresses: "fan@example.com" becomes "f**@example.com".</summary>
    public static string Mask(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0)
        {
            return "***";
        }

        return string.Concat(email.AsSpan(0, 1), new string('*', at - 1), email.AsSpan(at));
    }
}
