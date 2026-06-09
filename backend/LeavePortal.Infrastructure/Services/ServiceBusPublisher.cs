using System.Text.Json;
using LeavePortal.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeavePortal.Infrastructure.Services;

// STUB implementation for Day 3.
// The Azure Service Bus namespace/queue is not created until Day 5, so for now we just
// log the message that WOULD be published. The rest of the system (handlers, controllers)
// already talks to IServiceBusPublisher, so on Day 5 we replace ONLY this class with the
// real Azure.Messaging.ServiceBus implementation — nothing else changes.
public class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(ILogger<ServiceBusPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string queueOrTopic, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        _logger.LogInformation(
            "[ServiceBus STUB] Would publish to '{Queue}': {Payload}",
            queueOrTopic, json);

        // No real send yet. Returns immediately so the API stays fast and decoupled.
        return Task.CompletedTask;
    }
}
