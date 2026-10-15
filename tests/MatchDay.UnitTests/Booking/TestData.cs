using MatchDay.Booking.Api.Domain;

namespace MatchDay.UnitTests.Booking;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly MatchSection MainTribune = new(
        MatchId: 1,
        MatchTitle: "Trimontium Plovdiv vs Vitosha Sofia",
        KickOff: new DateTimeOffset(2026, 10, 31, 17, 30, 0, TimeSpan.Zero),
        Stadium: "Trimontium Arena",
        SectionId: 3,
        SectionName: "Main Tribune",
        UnitPrice: 36m);

    public static SeatHold Hold(params Seat[] seats) =>
        SeatHold.Create(
            MainTribune,
            seats.Length > 0 ? seats : [new Seat(5, 12), new Seat(5, 13)],
            "fan@example.com",
            Now,
            SeatHold.DefaultDuration);
}
