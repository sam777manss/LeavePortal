using System.Text.Json;
using Azure.Messaging.ServiceBus;
using LeavePortal.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeavePortal.Infrastructure.Services;

// REAL Azure Service Bus implementation (Day 5) — replaces the Day 3 stub.
// Handlers depend only on IServiceBusPublisher, so swapping this class in required
// NO handler changes. This is the decoupled architecture decision in practice.
public class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string queueOrTopic, T message, CancellationToken cancellationToken = default)
    {
        // ServiceBusClient is the heavyweight, thread-safe object (registered as a singleton).
        // A sender is lightweight, so creating one per publish is fine.
        await using var sender = _client.CreateSender(queueOrTopic);

        var json = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(sbMessage, cancellationToken);

        _logger.LogInformation("Published message to '{Queue}': {Payload}", queueOrTopic, json);
    }
}
