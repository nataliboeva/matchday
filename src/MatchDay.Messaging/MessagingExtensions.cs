using MatchDay.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class MessagingExtensions
{
    /// <summary>
    /// Connects to RabbitMQ through the Aspire client integration (connection string, health check,
    /// telemetry) and registers <see cref="IMessagePublisher"/>.
    /// </summary>
    public static IHostApplicationBuilder AddRabbitMqMessaging(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddRabbitMQClient(connectionName);
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return builder;
    }

    /// <summary>
    /// Registers a background consumer that delivers <typeparamref name="TMessage"/> from
    /// <paramref name="queueName"/> to <typeparamref name="THandler"/>.
    /// </summary>
    public static IHostApplicationBuilder AddMessageConsumer<TMessage, THandler>(this IHostApplicationBuilder builder, string queueName)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        builder.Services.AddScoped<IMessageHandler<TMessage>, THandler>();
        builder.Services.AddHostedService(services =>
            ActivatorUtilities.CreateInstance<RabbitMqConsumer<TMessage>>(services, queueName));

        return builder;
    }
}
