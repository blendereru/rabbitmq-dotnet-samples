## Alternate exchanges
Alternate exchanges in RabbitMQ were designated to capture the messages that the primary exchange was unable to 
route. It is different from [dead-letter exchanges](https://rabbitmq.com/docs/dlx), as they don't handle the case when the message could not be routed.

## How do they work ?
Alternate exchanges can be declared using a [policy](https://rabbitmq.com/docs/parameters#policies) or using client-provided
arguments(the one I use).
```csharp
var arguments = new Dictionary<string, object?>()
{
    {"alternate-exchange", "my-ae" }
};
await channel.ExchangeDeclareAsync("direct-exchange", ExchangeType.Direct, false, false, arguments);
await channel.ExchangeDeclareAsync("my-ae", ExchangeType.Fanout);
await channel.QueueDeclareAsync("routed", exclusive: false, autoDelete: false);
await channel.QueueBindAsync("routed", "direct-exchange", "key1");
await channel.QueueDeclareAsync("unrouted", exclusive: false, autoDelete: false);
await channel.QueueBindAsync("unrouted", "my-ae", string.Empty);
```
In this case, we bind the alternate exchange called `my-ae` to the exchange `direct-exchange`. So, when the messages that
are published to direct-exchange couldn't be routed to any queue, the exchange `my-ae` will capture them.

In the Publisher, we publish 2 messages to the `direct-exchange`, but the first one is published using the routing key
`key1` which will be successfully routed to `routed` queue. The second message will not, as the direct-exchange doesn't 
have any queue with the routing key `key2`. So instead of being silently discarded, these messages are published to `my-ae`
from where they are eventually delivered to `unrouted` queue. 

## Alternatives to alternate exchanges
Alternate exchanges is one way to solve the problem of losing the messages from misconfiguration. But we could publish
the messages to the exchange using the `mandatory` flag on the message set to true. In this case, the messages that couldn't 
be published are returned using `basic.return`. In this case, we could handle the message return using the BasicReturnAsync
event:
```dotnet
channel.BasicReturnAsync += async (model, ea) =>
{
    // the rest of the code
};
```
## Run the code
1) Run docker image of RabbitMQ(latest):
```ps
# latest RabbitMQ 4.0.x
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4.0-management
```
2) Run the Publisher, Main.Consumer(the consumer that consumes from the `routed` queue), AE.Consumer(the consumer
that consumes alternate exchange's messages) either from the IDE or from the command-line using the command
```ps
dotnet run
```
