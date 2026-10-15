using MatchDay.Booking.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MatchDay.Booking.Api.Data;

public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public const string IdempotencyKeyIndex = "IX_Bookings_IdempotencyKey";
    public const string HoldIdIndex = "IX_Bookings_HoldId";
    public const string SoldSeatIndex = "IX_BookedSeats_SoldSeat";

    public DbSet<TicketBooking> Bookings => Set<TicketBooking>();

    /// <summary>Returns the requested seats that are already sold for this match section.</summary>
    public async Task<IReadOnlyList<Seat>> FindSoldSeatsAsync(
        int matchId, int sectionId, IReadOnlyCollection<Seat> seats, CancellationToken cancellationToken)
    {
        var rows = seats.Select(s => s.Row).Distinct().ToArray();

        var soldInRows = await Bookings
            .SelectMany(b => b.Seats)
            .Where(s => s.MatchId == matchId && s.SectionId == sectionId && !s.IsReleased && rows.Contains(s.Row))
            .Select(s => new { s.Row, s.Number })
            .ToListAsync(cancellationToken);

        return [.. soldInRows.Select(s => new Seat(s.Row, s.Number)).Intersect(seats)];
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketBooking>(booking =>
        {
            booking.ToTable("Bookings");
            booking.HasKey(b => b.Id);

            booking.Property(b => b.IdempotencyKey).HasMaxLength(100);
            booking.HasIndex(b => b.IdempotencyKey).IsUnique().HasDatabaseName(IdempotencyKeyIndex);
            booking.HasIndex(b => b.HoldId).IsUnique().HasDatabaseName(HoldIdIndex);

            booking.Property(b => b.CustomerEmail).HasMaxLength(254);
            booking.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            booking.ComplexProperty(b => b.Section, section =>
            {
                section.Property(s => s.MatchTitle).HasMaxLength(130);
                section.Property(s => s.Stadium).HasMaxLength(100);
                section.Property(s => s.SectionName).HasMaxLength(60);
                section.Property(s => s.UnitPrice).HasPrecision(10, 2);
            });

            booking.ComplexProperty(b => b.Price, price =>
            {
                price.Property(p => p.UnitPrice).HasPrecision(10, 2);
                price.Property(p => p.Subtotal).HasPrecision(10, 2);
                price.Property(p => p.Discount).HasPrecision(10, 2);
                price.Property(p => p.ServiceFee).HasPrecision(10, 2);
                price.Property(p => p.Total).HasPrecision(10, 2);
            });

            booking.OwnsMany(b => b.Seats, seat =>
            {
                seat.ToTable("BookedSeats");
                seat.WithOwner().HasForeignKey("BookingId");
                seat.Property<int>("Id");
                seat.HasKey("Id");

                // The last line of defence against double-selling: a seat can only be sold once,
                // unless the booking that held it was cancelled.
                seat.HasIndex(s => new { s.MatchId, s.SectionId, s.Row, s.Number })
                    .IsUnique()
                    .HasFilter("\"IsReleased\" = false")
                    .HasDatabaseName(SoldSeatIndex);
            });

            booking.Navigation(b => b.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}

internal static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolationOf(this DbUpdateException exception, string indexName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == indexName;
}
