namespace MatchDay.Booking.Api.Domain;

/// <summary>A seat within a section, addressed by row and seat number (both starting at 1).</summary>
public readonly record struct Seat
{
    public Seat(int row, int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(row, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        Row = row;
        Number = number;
    }

    public int Row { get; }

    public int Number { get; }

    public override string ToString() => $"Row {Row}, Seat {Number}";
}
