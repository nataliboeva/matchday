using System.Text.Json;
using MatchDay.Booking.Api.Domain;
using StackExchange.Redis;

namespace MatchDay.Booking.Api.Holds;

/// <summary>
/// Keeps holds in Redis with a TTL. Each held seat gets its own key, and Lua scripts make
/// "check all seats are free, then lock them" a single atomic step, so two customers can never
/// hold the same seat - even when their requests arrive at the same moment.
/// </summary>
public sealed class RedisSeatHoldStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : ISeatHoldStore
{
    // KEYS[1] = hold key, KEYS[2..n] = seat keys; ARGV[1] = hold id, ARGV[2] = hold JSON, ARGV[3] = TTL in ms
    private const string CreateHoldScript = """
        for i = 2, #KEYS do
            if redis.call('EXISTS', KEYS[i]) == 1 then
                return 0
            end
        end
        for i = 2, #KEYS do
            redis.call('SET', KEYS[i], ARGV[1], 'PX', ARGV[3])
        end
        redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
        return 1
        """;

    // Only unlocks seats still owned by this hold, so a seat that expired and was held
    // by someone else in the meantime is left alone.
    private const string ReleaseHoldScript = """
        for i = 2, #KEYS do
            if redis.call('GET', KEYS[i]) == ARGV[1] then
                redis.call('DEL', KEYS[i])
            end
        end
        redis.call('DEL', KEYS[1])
        return 1
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> TryCreateAsync(SeatHold hold, CancellationToken cancellationToken)
    {
        var timeToLive = hold.ExpiresAt - timeProvider.GetUtcNow();
        if (timeToLive <= TimeSpan.Zero)
        {
            return false;
        }

        var json = JsonSerializer.Serialize(StoredHold.From(hold), SerializerOptions);
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            CreateHoldScript,
            KeysFor(hold),
            [hold.Id.ToString(), json, (long)timeToLive.TotalMilliseconds]);

        return (int)result == 1;
    }

    public async Task<SeatHold?> GetAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var json = await redis.GetDatabase().StringGetAsync(HoldKey(holdId));

        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<StoredHold>(json.ToString(), SerializerOptions)?.ToSeatHold();
    }

    public Task ReleaseAsync(SeatHold hold, CancellationToken cancellationToken) =>
        redis.GetDatabase().ScriptEvaluateAsync(ReleaseHoldScript, KeysFor(hold), [hold.Id.ToString()]);

    private static RedisKey[] KeysFor(SeatHold hold) =>
    [
        HoldKey(hold.Id),
        .. hold.Seats.Select(seat => SeatKey(hold.Section, seat)),
    ];

    private static RedisKey HoldKey(Guid holdId) => $"hold:{holdId}";

    private static RedisKey SeatKey(MatchSection section, Seat seat) =>
        $"seat:{section.MatchId}:{section.SectionId}:{seat.Row}:{seat.Number}";

    // The stored shape is kept separate from the domain type so the Redis format is explicit.
    private sealed record StoredHold(
        Guid Id,
        MatchSection Section,
        List<StoredSeat> Seats,
        string CustomerEmail,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt)
    {
        public static StoredHold From(SeatHold hold) => new(
            hold.Id,
            hold.Section,
            [.. hold.Seats.Select(s => new StoredSeat(s.Row, s.Number))],
            hold.CustomerEmail,
            hold.CreatedAt,
            hold.ExpiresAt);

        public SeatHold ToSeatHold() => new()
        {
            Id = Id,
            Section = Section,
            Seats = [.. Seats.Select(s => new Seat(s.Row, s.Number))],
            CustomerEmail = CustomerEmail,
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
        };
    }

    private sealed record StoredSeat(int Row, int Number);
}
