using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup. That keeps the demo self-contained with one replica
    /// per service; a production deployment would run migrations as a separate step instead.
    /// </summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        // On a brand-new database EF Core queries the migrations history table before creating it,
        // which shows up as an (harmless) error in the dashboard. Creating it first keeps startup clean.
        await db.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
