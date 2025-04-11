## Super Streams
Super Streams model exchange to queue binding except queues here are streams(called `partitions`) and the 
exchange is the actual `super stream`. So when we publish to the super stream it routes the messages to the
partitions based on the `routing key` of the message. There are 2 ways to configure the routing key:
* Based on `hash` - the client will hash the routing key to determine the stream to send the message to(using partition list and a modulo operation)
* Based on `key` - when `partitions` are bound to the super stream using specific `key` value. In
this case producer itself is responsible to routing the message. if the key is not found `RouRouteNotFoundException`
is thrown.

## Declaring
Producer configuration doesn't change, except if it wants to enable routing based on `key` it has
to set the `SuperStreamConfig` property:
```csharp
var producer = await Producer.Create(new ProducerConfig(streamSystem, "invoices")
{
    SuperStreamConfig = new SuperStreamConfig()
    {
        Routing = msg => msg.Properties.MessageId.ToString(),
        //RoutingStrategyType = RoutingStrategyType.Key
    }
}).ConfigureAwait(false);
```
Here, `invoices` is the super stream. Its creation can be done in 2 ways:
1) 
```csharp
await streamSystem.CreateSuperStream(new PartitionsSuperStreamSpec("invoices", 3));
```
In this case, we specify that we are using `PartitionsSuperStreamSpec`, which is based on hashing

2) 
```bash
rabbitmq-streams add_super_stream invoices --partitions 3
```

Or, if we use `key` based routing:
```csharp
await streamSystem.CreateSuperStream(new BindingsSuperStreamSpec("invoices", new []{"amer", "emea", "apac"}));
```
In this case, we specify 3 custom routing keys to partitions for the case if we want to manage the routing
key definition ourselves. The same configuration using `CLI`:
```bash
rabbitmq-streams add_super_stream invoices  --routing-keys apac,emea,amer
```

So, coming to our producer definition, we set the `hash-routing` mechanism, hashing the `MessageId`
property on each message:
```csharp
SuperStreamConfig = new SuperStreamConfig()
{
    Routing = msg => msg.Properties.MessageId.ToString(),
    //RoutingStrategyType = RoutingStrategyType.Key
}
```
Note that producer, as much as consumer must never know about the existence of `partitions`(streams).
They assume that the configured super stream is the stream to consume from.

To declare the consumer, we just have to set `IsSuperStream` property to true:
```csharp
var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, "invoices")
{
    IsSuperStream = true,
    Reference = "my_consumer",
    OffsetSpec = new OffsetTypeFirst(),
    MessageHandler = async (stream, consumerSource, context, message) =>
    {
        var body = Encoding.UTF8.GetString(message.Data.Contents);
        Console.WriteLine($"Received message id: {message.Properties.MessageId}, body: {body}, stream: {stream}, offset: {context.Offset}");
        // if (message.ApplicationProperties.ContainsKey("Id") && message.ApplicationProperties["Id"] is int id)
        // {
        //     if (id % 25 == 0) // store offset for every 25 messages
        //     {
        //         await consumerSource.StoreOffset((ulong)id).ConfigureAwait(false);
        //     }
        // }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}).ConfigureAwait(false);
```
In this case, internally 3 `consumers` are created by the system to consume for each `partition`, and 
messages will flow in your `MessageHandler`.

One thing to note about Super Streams, is the way offset tracking works: The offset tracking is per stream.