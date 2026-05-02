using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class InMemoryIntegrationEventPublisher(
    InMemoryIntegrationEventBus bus,
    ILogger<InMemoryIntegrationEventPublisher> logger
) : IIntegrationEventPublisher
{
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventRoutingKey routingKey,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "In-memory publishing {IntegrationEvent} to {RoutingKey}",
            integrationEvent,
            routingKey
        );

        await bus.PublishAsync(integrationEvent, routingKey, cancellationToken);
    }
}
