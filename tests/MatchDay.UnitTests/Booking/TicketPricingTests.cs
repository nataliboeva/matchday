using MatchDay.Booking.Api.Domain;

namespace MatchDay.UnitTests.Booking;

public class TicketPricingTests
{
    [Fact]
    public void Small_orders_pay_the_ticket_price_plus_a_service_fee_per_ticket()
    {
        var price = TicketPricing.Calculate(unitPrice: 21m, quantity: 2);

        price.ShouldBe(new PriceBreakdown(UnitPrice: 21m, Quantity: 2, Subtotal: 42m, Discount: 0m, ServiceFee: 3m, Total: 45m));
    }

    [Fact]
    public void Group_discount_applies_to_tickets_but_not_to_fees()
    {
        var price = TicketPricing.Calculate(unitPrice: 36m, quantity: 4);

        price.Subtotal.ShouldBe(144m);
        price.Discount.ShouldBe(14.40m);
        price.ServiceFee.ShouldBe(6m);
        price.Total.ShouldBe(135.60m);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 5.20)]
    [InlineData(6, 7.80)]
    public void Group_discount_starts_at_four_tickets(int quantity, decimal expectedDiscount)
    {
        TicketPricing.Calculate(unitPrice: 13m, quantity).Discount.ShouldBe(expectedDiscount);
    }

    [Fact]
    public void Discount_is_rounded_half_away_from_zero()
    {
        // 5 x 12.33 = 61.65, 10% = 6.165 -> 6.17 (banker's rounding would give 6.16)
        var price = TicketPricing.Calculate(unitPrice: 12.33m, quantity: 5);

        price.Discount.ShouldBe(6.17m);
        price.Total.ShouldBe(61.65m - 6.17m + 7.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity)
    {
        var calculate = () => TicketPricing.Calculate(10m, quantity);

        calculate.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Unit_price_cannot_be_negative()
    {
        var calculate = () => TicketPricing.Calculate(-0.01m, 1);

        calculate.ShouldThrow<ArgumentOutOfRangeException>();
    }
}
