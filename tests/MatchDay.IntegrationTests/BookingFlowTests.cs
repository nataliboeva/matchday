using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;

namespace MatchDay.IntegrationTests;

/// <summary>
/// End-to-end tests against the real distributed app: catalog -> hold (Redis) -> confirm (PostgreSQL)
/// -> BookingConfirmed (RabbitMQ) -> e-ticket (notifications worker).
/// </summary>
public sealed class BookingFlowTests(MatchDayAppFixture fixture)
{
    private const int MatchId = 1;
    private const int MainTribune = 3;
    private const int NorthStand = 1;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Catalog_lists_the_seeded_fixtures()
    {
        Assert.SkipUnless(DockerAvailability.IsAvailable, DockerAvailability.SkipReason);
        using var catalog = fixture.CreateHttpClient("catalog-api");

        var matches = await catalog.GetFromJsonAsync<List<MatchSummary>>("/matches", Json, CancellationToken);

        matches.ShouldNotBeNull();
        matches.Count.ShouldBe(7);
        matches.ShouldContain(m => m.HomeTeam == "Trimontium Plovdiv" && m.AwayTeam == "Vitosha Sofia");
        matches.ShouldBeInOrder(SortDirection.Ascending, Comparer<MatchSummary>.Create((a, b) => a.KickOff.CompareTo(b.KickOff)));
    }

    [Fact]
    public async Task Hold_then_confirm_issues_an_e_ticket_within_the_same_trace()
    {
        Assert.SkipUnless(DockerAvailability.IsAvailable, DockerAvailability.SkipReason);
        using var booking = fixture.CreateHttpClient("booking-api");

        var hold = await HoldAsync(booking, MainTribune, new Seat(1, 1), new Seat(1, 2));
        hold.Price.Total.ShouldBe(2 * 36m + 2 * 1.50m);

        // Start a trace here; HttpClient propagates it as a W3C traceparent header, and the services
        // must carry it over HTTP and through RabbitMQ into the worker.
        using var trace = new Activity("integration-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        trace.ActivityTraceFlags = ActivityTraceFlags.Recorded;

        var response = await ConfirmAsync(booking, hold.HoldId, idempotencyKey: Guid.NewGuid().ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var confirmed = await response.Content.ReadFromJsonAsync<BookingDto>(Json, CancellationToken);
        confirmed.ShouldNotBeNull();
        confirmed.Status.ShouldBe("Confirmed");

        var ticket = await WaitForETicketAsync(confirmed.Id);
        ticket.TicketCode.ShouldStartWith("MD-");
        ticket.TraceId.ShouldBe(trace.TraceId.ToHexString());
    }

    [Fact]
    public async Task Two_customers_cannot_hold_the_same_seat()
    {
        Assert.SkipUnless(DockerAvailability.IsAvailable, DockerAvailability.SkipReason);
        using var booking = fixture.CreateHttpClient("booking-api");
        var seat = new Seat(2, 7);

        // Fire both requests at once: the Redis script must let exactly one of them win.
        var attempts = await Task.WhenAll(
            booking.PostAsJsonAsync("/holds", HoldRequest(NorthStand, seat), Json, CancellationToken),
            booking.PostAsJsonAsync("/holds", HoldRequest(NorthStand, seat), Json, CancellationToken));

        attempts.Select(r => r.StatusCode).ShouldBe([HttpStatusCode.Created, HttpStatusCode.Conflict], ignoreOrder: true);
    }

    [Fact]
    public async Task Retrying_a_confirmation_with_the_same_idempotency_key_returns_the_same_booking()
    {
        Assert.SkipUnless(DockerAvailability.IsAvailable, DockerAvailability.SkipReason);
        using var booking = fixture.CreateHttpClient("booking-api");
        var hold = await HoldAsync(booking, MainTribune, new Seat(3, 5));
        var idempotencyKey = Guid.NewGuid().ToString();

        var first = await ConfirmAsync(booking, hold.HoldId, idempotencyKey);
        var retry = await ConfirmAsync(booking, hold.HoldId, idempotencyKey);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBooking = await first.Content.ReadFromJsonAsync<BookingDto>(Json, CancellationToken);
        var retriedBooking = await retry.Content.ReadFromJsonAsync<BookingDto>(Json, CancellationToken);
        retriedBooking!.Id.ShouldBe(firstBooking!.Id);
    }

    [Fact]
    public async Task Sold_seats_cannot_be_held_again()
    {
        Assert.SkipUnless(DockerAvailability.IsAvailable, DockerAvailability.SkipReason);
        using var booking = fixture.CreateHttpClient("booking-api");
        var seat = new Seat(4, 20);
        var hold = await HoldAsync(booking, MainTribune, seat);
        (await ConfirmAsync(booking, hold.HoldId, Guid.NewGuid().ToString())).StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondHold = await booking.PostAsJsonAsync("/holds", HoldRequest(MainTribune, seat), Json, CancellationToken);

        secondHold.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await secondHold.Content.ReadFromJsonAsync<ProblemDto>(Json, CancellationToken);
        problem!.Title.ShouldBe("Seats are already sold.");
    }

    private static async Task<HoldDto> HoldAsync(HttpClient booking, int sectionId, params Seat[] seats)
    {
        var response = await booking.PostAsJsonAsync("/holds", HoldRequest(sectionId, seats), Json, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(CancellationToken));

        var hold = await response.Content.ReadFromJsonAsync<HoldDto>(Json, CancellationToken);
        return hold.ShouldNotBeNull();
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient booking, Guid holdId, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bookings")
        {
            Content = JsonContent.Create(new { holdId }, options: Json),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return booking.SendAsync(request, CancellationToken);
    }

    private static object HoldRequest(int sectionId, params Seat[] seats) => new
    {
        matchId = MatchId,
        sectionId,
        customerEmail = "fan@example.com",
        seats,
    };

    private async Task<ETicketRow> WaitForETicketAsync(Guid bookingId)
    {
        await using var db = await fixture.OpenDatabaseAsync("notificationsdb", CancellationToken);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var query = new NpgsqlCommand(
                """SELECT "TicketCode", "TraceId" FROM "ETickets" WHERE "BookingId" = @bookingId""", db);
            query.Parameters.AddWithValue("bookingId", bookingId);

            await using var reader = await query.ExecuteReaderAsync(CancellationToken);
            if (await reader.ReadAsync(CancellationToken))
            {
                return new ETicketRow(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken);
        }

        throw new TimeoutException($"No e-ticket was issued for booking {bookingId} within 30 seconds.");
    }

    private sealed record Seat(int Row, int Number);

    private sealed record MatchSummary(int Id, string HomeTeam, string AwayTeam, DateTimeOffset KickOff);

    private sealed record Price(decimal Total);

    private sealed record HoldDto(Guid HoldId, Price Price, DateTimeOffset ExpiresAt);

    private sealed record BookingDto(Guid Id, string Status);

    private sealed record ProblemDto(string Title, int Status);

    private sealed record ETicketRow(string TicketCode, string? TraceId);
}
