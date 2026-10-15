using MatchDay.Messaging.Contracts;
using MatchDay.Notifications.Worker.Data;
using MatchDay.Notifications.Worker.Tickets;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<NotificationsDbContext>("notificationsdb");
builder.AddRabbitMqMessaging("messaging");
builder.AddMessageConsumer<BookingConfirmed, BookingConfirmedHandler>("notifications.booking-confirmed");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITicketSender, LoggingTicketSender>();

var host = builder.Build();

await host.MigrateDatabaseAsync<NotificationsDbContext>();
await host.RunAsync();
