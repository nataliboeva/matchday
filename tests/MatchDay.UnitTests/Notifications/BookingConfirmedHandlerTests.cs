using MatchDay.Messaging;
using MatchDay.Messaging.Contracts;
using MatchDay.Notifications.Worker.Data;
using MatchDay.Notifications.Worker.Tickets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace MatchDay.UnitTests.Notifications;

public sealed class BookingConfirmedHandlerTests : IDisposable
{
    private static readonly BookingConfirmed Message = new(
        BookingId: Guid.Parse("01a13f4d-c883-7102-a375-9f996096d7cb"),
        CustomerEmail: "fan@example.com",
        MatchId: 1,
        MatchTitle: "Trimontium Plovdiv vs Vitosha Sofia",
        KickOff: new DateTimeOffset(2026, 10, 31, 17, 30, 0, TimeSpan.Zero),
        Stadium: "Trimontium Arena",
        Section: "Main Tribune",
        Seats: ["Row 5, Seat 12", "Row 5, Seat 13"],
        TotalPrice: 75m,
        Currency: "EUR",
        ConfirmedAt: new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));

    // SQLite in memory stands in for PostgreSQL; the connection must stay open for the database to live.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly RecordingTicketSender _sender = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 12, 0, 5, TimeSpan.Zero));

    public BookingConfirmedHandlerTests()
    {
        _connection.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Issues_sends_and_stores_an_e_ticket()
    {
        await HandleAsync(new MessageContext("message-1", Redelivered: false, TraceId: "0af7651916cd43dd8448eb211c80319c"));

        var sent = _sender.Sent.ShouldHaveSingleItem();
        sent.TicketCode.ShouldBe("MD-996096D7CB");

        await using var db = CreateDbContext();
        var stored = (await db.Tickets.ToListAsync(TestContext.Current.CancellationToken)).ShouldHaveSingleItem();
        stored.BookingId.ShouldBe(Message.BookingId);
        stored.Seats.ShouldBe(["Row 5, Seat 12", "Row 5, Seat 13"]);
        stored.TraceId.ShouldBe("0af7651916cd43dd8448eb211c80319c");
        stored.SentAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public async Task A_redelivered_booking_is_not_sent_twice()
    {
        await HandleAsync(new MessageContext("message-1", Redelivered: false, TraceId: null));
        await HandleAsync(new MessageContext("message-1", Redelivered: true, TraceId: null));

        _sender.Sent.Count.ShouldBe(1);
        await using var db = CreateDbContext();
        (await db.Tickets.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData("fan@example.com", "f**@example.com")]
    [InlineData("n@example.com", "n@example.com")]
    [InlineData("not-an-email", "***")]
    public void Email_addresses_are_masked_in_logs(string email, string expected)
    {
        EmailMask.Mask(email).ShouldBe(expected);
    }

    public void Dispose() => _connection.Dispose();

    private async Task HandleAsync(MessageContext context)
    {
        // A fresh DbContext per message, like the consumer's DI scope
        await using var db = CreateDbContext();
        var handler = new BookingConfirmedHandler(db, _sender, _time, NullLogger<BookingConfirmedHandler>.Instance);

        await handler.HandleAsync(Message, context, TestContext.Current.CancellationToken);
    }

    private NotificationsDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>().UseSqlite(_connection).Options);

    private sealed class RecordingTicketSender : ITicketSender
    {
        public List<ETicket> Sent { get; } = [];

        public Task SendAsync(ETicket ticket, CancellationToken cancellationToken)
        {
            Sent.Add(ticket);
            return Task.CompletedTask;
        }
    }
}
