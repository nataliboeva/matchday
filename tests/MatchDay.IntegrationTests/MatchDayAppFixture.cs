using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

[assembly: AssemblyFixture(typeof(MatchDay.IntegrationTests.MatchDayAppFixture))]

namespace MatchDay.IntegrationTests;

/// <summary>
/// Starts the whole AppHost once for all tests: containers, services and the worker.
/// Tests share it, so each test books its own seats.
/// </summary>
public sealed class MatchDayAppFixture : IAsyncLifetime
{
    private const string NotificationsQueue = "notifications.booking-confirmed";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? _app;

    public DistributedApplication App =>
        _app ?? throw new InvalidOperationException("The app isn't running. " + DockerAvailability.SkipReason);

    public async ValueTask InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        using var timeout = new CancellationTokenSource(StartupTimeout);
        var cancellationToken = timeout.Token;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MatchDay_AppHost>(cancellationToken);
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        builder.Services.AddLogging(logging => logging
            .AddFilter("Aspire", LogLevel.Warning)
            .AddFilter(builder.Environment.ApplicationName, LogLevel.Warning)
            // Health probes are expected to fail while the containers are still starting
            .AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.None)
            .AddFilter("Polly", LogLevel.Error));

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("catalog-api", cancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("booking-api", cancellationToken);
        await WaitForConsumerAsync(NotificationsQueue, cancellationToken);
    }

    public HttpClient CreateHttpClient(string resourceName) => App.CreateHttpClient(resourceName, "http");

    public async Task<NpgsqlConnection> OpenDatabaseAsync(string databaseName, CancellationToken cancellationToken)
    {
        var connectionString = await App.GetConnectionStringAsync(databaseName, cancellationToken);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// The worker has no HTTP endpoint to probe; it is ready once its queue exists and has a consumer.
    /// Without this, a booking made too early would be published before anyone listens.
    /// </summary>
    private async Task WaitForConsumerAsync(string queueName, CancellationToken cancellationToken)
    {
        var connectionString = await App.GetConnectionStringAsync("messaging", cancellationToken);
        var factory = new ConnectionFactory { Uri = new Uri(connectionString!) };
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);

        while (true)
        {
            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                var queue = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken);
                if (queue.ConsumerCount > 0)
                {
                    return;
                }
            }
            catch (OperationInterruptedException)
            {
                // The queue hasn't been declared yet
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }
}
