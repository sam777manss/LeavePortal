namespace LeavePortal.Core.Interfaces;

// Abstraction over the message bus.
// Day 3: a stub implementation just logs the message (Service Bus namespace is not created yet).
// Day 5: the real Azure Service Bus implementation is swapped in — no handler code changes,
//        because handlers depend on THIS interface, not on Azure SDK types.
// This is the "decoupled / async" architecture decision from the TechDoc in practice.
public interface IServiceBusPublisher
{
    Task PublishAsync<T>(string queueOrTopic, T message, CancellationToken cancellationToken = default);
}
