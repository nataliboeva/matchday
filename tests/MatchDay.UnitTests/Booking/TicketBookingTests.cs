using MatchDay.Booking.Api.Domain;

namespace MatchDay.UnitTests.Booking;

public class TicketBookingTests
{
    [Fact]
    public void Confirm_creates_a_confirmed_booking_from_an_active_hold()
    {
        var hold = TestData.Hold(new Seat(5, 12), new Seat(5, 13));
        var confirmedAt = TestData.Now.AddMinutes(3);

        var booking = TicketBooking.Confirm(hold, "key-1", confirmedAt);

        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.HoldId.ShouldBe(hold.Id);
        booking.IdempotencyKey.ShouldBe("key-1");
        booking.ConfirmedAt.ShouldBe(confirmedAt);
        booking.CustomerEmail.ShouldBe("fan@example.com");
        booking.Price.ShouldBe(hold.Price);
        booking.Seats.Select(s => s.ToSeat()).ShouldBe([new Seat(5, 12), new Seat(5, 13)]);
        booking.Seats.ShouldAllBe(s => s.MatchId == 1 && s.SectionId == 3 && !s.IsReleased);
    }

    [Fact]
    public void Confirm_rejects_an_expired_hold()
    {
        var hold = TestData.Hold();

        var confirm = () => TicketBooking.Confirm(hold, "key-1", hold.ExpiresAt);

        confirm.ShouldThrow<DomainException>().Message.ShouldContain("expired");
    }

    [Fact]
    public void Confirm_requires_an_idempotency_key()
    {
        var confirm = () => TicketBooking.Confirm(TestData.Hold(), " ", TestData.Now);

        confirm.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Cancel_moves_the_booking_to_cancelled_and_releases_its_seats()
    {
        var booking = TicketBooking.Confirm(TestData.Hold(), "key-1", TestData.Now);
        var cancelledAt = TestData.Now.AddDays(1);

        booking.Cancel(cancelledAt);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledAt.ShouldBe(cancelledAt);
        booking.Seats.ShouldAllBe(s => s.IsReleased);
    }

    [Fact]
    public void Cancel_twice_is_rejected()
    {
        var booking = TicketBooking.Confirm(TestData.Hold(), "key-1", TestData.Now);
        booking.Cancel(TestData.Now.AddHours(1));

        var cancelAgain = () => booking.Cancel(TestData.Now.AddHours(2));

        cancelAgain.ShouldThrow<DomainException>().Message.ShouldContain("already cancelled");
    }

    [Fact]
    public void Cancel_after_kick_off_is_rejected()
    {
        var booking = TicketBooking.Confirm(TestData.Hold(), "key-1", TestData.Now);

        var cancel = () => booking.Cancel(TestData.MainTribune.KickOff);

        cancel.ShouldThrow<DomainException>();
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.Seats.ShouldAllBe(s => !s.IsReleased);
    }
}
