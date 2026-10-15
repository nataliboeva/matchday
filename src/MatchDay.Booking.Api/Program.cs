using MatchDay.Booking.Api.Catalog;
using MatchDay.Booking.Api.ErrorHandling;
using MatchDay.Booking.Api.Holds;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");

builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISeatHoldStore, RedisSeatHoldStore>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHoldEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();
