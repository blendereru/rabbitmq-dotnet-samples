using System.Buffers;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, "my-sac-stream")
{
    Reference = "sac_consumer",
    OffsetSpec = new OffsetTypeFirst(),
    IsSingleActiveConsumer = true,
    MessageHandler = async (_, consumer, context, message) =>
    {
        var text = Encoding.UTF8.GetString(message.Data.Contents.ToArray());
        Console.WriteLine($"The message {text} was received");
        if (message.ApplicationProperties.ContainsKey("Id") && message.ApplicationProperties["Id"] is int id)
        {
            if (id % 25 == 0)
            {
                await consumer.StoreOffset((ulong)id);
            }
        }
        await Task.CompletedTask.ConfigureAwait(false);
    },
    ConsumerUpdateListener = async (consumerRef, stream, isActive) =>
    {
        try
        {
            var offset = await streamSystem.QueryOffset(consumerRef, stream).ConfigureAwait(false);
            return new OffsetTypeOffset(offset);
        }
        catch (OffsetNotFoundException)
        {
            Console.WriteLine(
                $"Offset not found for stream {stream} and consumer {consumerRef}. Will use the first offset");
            return new OffsetTypeFirst();
        }
    }
}).ConfigureAwait(false);
Console.WriteLine("Consumer is running. Press [enter] to exit.");
Console.ReadLine();
await consumer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);