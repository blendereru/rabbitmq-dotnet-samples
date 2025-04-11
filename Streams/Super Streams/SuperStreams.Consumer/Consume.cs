using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
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
Console.WriteLine("Waiting for message. Press [enter] to exit");
Console.ReadLine();
await consumer.Close().ConfigureAwait(false);
await consumer.Close().ConfigureAwait(false);