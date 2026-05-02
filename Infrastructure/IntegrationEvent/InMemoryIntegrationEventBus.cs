using Application.IntegrationEvent;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Infrastructure.IntegrationEvent;

public class InMemoryIntegrationEventBus(IOptions<InMemoryIntegrationEventBusOptions> options)
{
    private readonly Channel<IIntegrationEvent> _channel = Channel.CreateUnbounded<IIntegrationEvent>();
    private readonly string[] _routingKeys = options.Value.RoutingKeys;

    public ChannelReader<IIntegrationEvent> Reader => _channel.Reader;

    public async Task PublishAsync(IIntegrationEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
    {
        if (_routingKeys.Any(pattern => Matches(routingKey.Split('.'), 0, pattern.Split('.'), 0)))
            await _channel.Writer.WriteAsync(integrationEvent, cancellationToken);
    }

    private static bool Matches(string[] key, int ki, string[] pattern, int pi)
    {
        if (pi == pattern.Length)
            return ki == key.Length;

        if (pattern[pi] == "#")
        {
            for (var i = ki; i <= key.Length; i++)
                if (Matches(key, i, pattern, pi + 1)) return true;
            return false;
        }

        if (ki == key.Length) return false;

        if (pattern[pi] == "*" || pattern[pi] == key[ki])
            return Matches(key, ki + 1, pattern, pi + 1);

        return false;
    }
}

public class InMemoryIntegrationEventBusOptions
{
    public const string SectionName = "InMemoryIntegrationEventConsumer";
    public string[] RoutingKeys { get; init; } = [];
}
