using MatchDay.Booking.Api.Domain;

namespace MatchDay.UnitTests.Booking;

public class SeatHoldTests
{
    [Fact]
    public void Create_sets_expiry_relative_to_now()
    {
        var hold = TestData.Hold();

        hold.CreatedAt.ShouldBe(TestData.Now);
        hold.ExpiresAt.ShouldBe(TestData.Now.AddMinutes(10));
    }

    [Fact]
    public void Create_orders_seats_and_trims_the_email()
    {
        var hold = SeatHold.Create(
            TestData.MainTribune,
            [new Seat(6, 2), new Seat(5, 14), new Seat(5, 13)],
            "  fan@example.com ",
            TestData.Now,
            SeatHold.DefaultDuration);

        hold.Seats.ShouldBe([new Seat(5, 13), new Seat(5, 14), new Seat(6, 2)]);
        hold.CustomerEmail.ShouldBe("fan@example.com");
    }

    [Fact]
    public void Create_rejects_an_empty_seat_list()
    {
        var create = () => SeatHold.Create(TestData.MainTribune, [], "fan@example.com", TestData.Now, SeatHold.DefaultDuration);

        create.ShouldThrow<DomainException>();
    }

    [Fact]
    public void Create_rejects_more_seats_than_allowed_per_booking()
    {
        var seats = Enumerable.Range(1, SeatHold.MaxSeats + 1).Select(n => new Seat(1, n)).ToArray();

        var create = () => SeatHold.Create(TestData.MainTribune, seats, "fan@example.com", TestData.Now, SeatHold.DefaultDuration);

        create.ShouldThrow<DomainException>().Message.ShouldContain("between 1 and 6");
    }

    [Fact]
    public void Create_rejects_the_same_seat_twice()
    {
        var create = () => TestData.Hold(new Seat(5, 12), new Seat(5, 12));

        create.ShouldThrow<DomainException>();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(599, false)]
    [InlineData(600, true)]
    [InlineData(601, true)]
    public void IsExpired_once_the_hold_duration_has_passed(int secondsLater, bool expected)
    {
        var hold = TestData.Hold();

        hold.IsExpired(TestData.Now.AddSeconds(secondsLater)).ShouldBe(expected);
    }

    [Fact]
    public void Price_is_calculated_from_the_locked_unit_price()
    {
        var hold = TestData.Hold(new Seat(1, 1), new Seat(1, 2), new Seat(1, 3));

        hold.Price.UnitPrice.ShouldBe(36m);
        hold.Price.Quantity.ShouldBe(3);
        hold.Price.Total.ShouldBe(112.50m);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Seat_rows_and_numbers_start_at_one(int row, int number)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Seat(row, number));
    }
}
