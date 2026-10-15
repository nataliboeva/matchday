using MatchDay.Messaging.Contracts;
using MatchDay.Notifications.Worker.Data;
using MatchDay.Notifications.Worker.Tickets;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<NotificationsDbContext>("notificationsdb");
builder.AddRabbitMqMessaging("messaging");
builder.AddMessageConsumer<BookingConfirmed, BookingConfirmedHandler>("notifications.booking-confirmed");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITicketSender, LoggingTicketSender>();

var host = builder.Build();

// Migrating on startup keeps the demo self-contained (see "Design decisions" in the README).
await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
