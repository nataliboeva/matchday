namespace MatchDay.Booking.Api.Domain;

public sealed record PriceBreakdown(
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    decimal Discount,
    decimal ServiceFee,
    decimal Total);

/// <summary>
/// Pricing rules: a flat service fee per ticket, and a group discount on the tickets
/// (not on the fees) when several seats are bought together.
/// </summary>
public static class TicketPricing
{
    public const string Currency = "EUR";
    public const decimal ServiceFeePerTicket = 1.50m;
    public const int GroupDiscountMinTickets = 4;
    public const decimal GroupDiscountRate = 0.10m;

    public static PriceBreakdown Calculate(decimal unitPrice, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var subtotal = unitPrice * quantity;
        var discount = quantity >= GroupDiscountMinTickets ? RoundToCents(subtotal * GroupDiscountRate) : 0m;
        var serviceFee = ServiceFeePerTicket * quantity;

        return new PriceBreakdown(unitPrice, quantity, subtotal, discount, serviceFee, subtotal - discount + serviceFee);
    }

    // Money is rounded the way people expect (half a cent rounds up), not with banker's rounding.
    private static decimal RoundToCents(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
