using MatchDay.Notifications.Worker.Tickets;
using Microsoft.EntityFrameworkCore;

namespace MatchDay.Notifications.Worker.Data;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<ETicket> Tickets => Set<ETicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ETicket>(ticket =>
        {
            ticket.ToTable("ETickets");

            // One e-ticket per booking - this is what makes message handling idempotent.
            ticket.HasIndex(t => t.BookingId).IsUnique();
            ticket.HasIndex(t => t.TicketCode).IsUnique();

            ticket.Property(t => t.TicketCode).HasMaxLength(20);
            ticket.Property(t => t.CustomerEmail).HasMaxLength(254);
            ticket.Property(t => t.MatchTitle).HasMaxLength(130);
            ticket.Property(t => t.Stadium).HasMaxLength(100);
            ticket.Property(t => t.Section).HasMaxLength(60);
            ticket.Property(t => t.Currency).HasMaxLength(3);
            ticket.Property(t => t.MessageId).HasMaxLength(64);
            ticket.Property(t => t.TraceId).HasMaxLength(32);
            ticket.Property(t => t.TotalPrice).HasPrecision(10, 2);
        });
    }
}
