using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class InMemoryIntegrationEventConsumer(
    InMemoryIntegrationEventBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryIntegrationEventConsumer> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("IntegrationEventConsumer started");

        await foreach (var integrationEvent in bus.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                logger.LogInformation("In-memory consuming {IntegrationEvent}", integrationEvent);

                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
                await dispatcher.DispatchAsync(integrationEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {IntegrationEvent}", integrationEvent);
            }
        }

        logger.LogInformation("IntegrationEventConsumer stopped");
    }
}
