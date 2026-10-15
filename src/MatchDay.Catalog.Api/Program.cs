using MatchDay.Catalog.Api.Data;
using MatchDay.Catalog.Api.Matches;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapMatchEndpoints();
app.MapDefaultEndpoints();

await app.MigrateDatabaseAsync<CatalogDbContext>();
await app.RunAsync();
