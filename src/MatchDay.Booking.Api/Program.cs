using System.Text.Json.Serialization;
using MatchDay.Booking.Api.Bookings;
using MatchDay.Booking.Api.Catalog;
using MatchDay.Booking.Api.Data;
using MatchDay.Booking.Api.ErrorHandling;
using MatchDay.Booking.Api.Holds;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<BookingDbContext>("bookingdb");
builder.AddRedisClient("redis");
builder.AddRabbitMqMessaging("messaging");

builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISeatHoldStore, RedisSeatHoldStore>();
builder.Services.AddSingleton<BookingMetrics>();
builder.Services.AddScoped<BookingService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHoldEndpoints();
app.MapBookingEndpoints();
app.MapDefaultEndpoints();

// Migrating on startup keeps the demo self-contained (see "Design decisions" in the README).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();
