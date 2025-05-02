## Consumer cancellation notifications
Consumer cancellation notifications are triggered in the following cases:
1) The client application explicitly cancels the consumer by invoking the `basic.cancel` method
2) If the queue a consumer is subscribed to is deleted(during the consumption process)
3) In a clustered environment, if the node hosting the queue fails or becomes unreachable, the broker cancels the consumer.

## How does it work ?
As established, a cancellation can be initiated by either the **client** or the **broker**.
1) Client-initiated cancellation: The client application explicitly cancels the consumer
by invoking the `basic.cancel` method, providing the `consumer_tag` that identifies the consumer.
Upon receiving this request, the broker stops delivering messages to the consumer and responds with a
`basic.cancel-ok` method to acknowledge the cancellation. 
2) Broker-Initiated cancellation: In these cases, the broker initiates a `basic.cancel` method to notify the
client that the consumer has been canceled unexpectedly. By default, AMQP 0-9-1 clients do not expect 
asynchronous `basic.cancel` messages from the broker. To receive these notifications, the client must declare
support for the `consumer_cancel_notify` capability during the connection setup. Once this capability is
declared, the broker will send a `basic.cancel` method to the client upon unexpected consumer cancellations.

## Example in .NET client
By default, `AsyncEventingBasicConsumer` doesn't provide any implementation for requested cancellation, so we have
to provide our own implementation by extending the class and overriding the following method:
```csharp
public override async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = new CancellationToken())
{
    Console.WriteLine($"Consumer '{consumerTag}' has been cancelled unexpectedly.");
    await Task.CompletedTask;
}
```
In this case, if during the consumption, we delete the queue we receive the cancellation notification. We could 
also implement the `handleCancelOk(String consumerTag)` for client-initiated cancellations, if needed, but for
the sake of an example that's enough.