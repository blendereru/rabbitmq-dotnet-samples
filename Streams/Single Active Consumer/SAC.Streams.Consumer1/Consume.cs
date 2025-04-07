using System.Buffers;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, "my-sac-stream")
{
    Reference = "first_consumer",
    OffsetSpec = new OffsetTypeFirst(),
    IsSingleActiveConsumer = true,
    MessageHandler = async (_, consumer, context, message) =>
    {
        Console.WriteLine(consumer.Info.Reference);
        var text = Encoding.UTF8.GetString(message.Data.Contents.ToArray());
        Console.WriteLine($"The message {text} was received");
        await Task.CompletedTask.ConfigureAwait(false);
    },
    ConsumerUpdateListener = async (consumerRef, stream, isActive) =>
    {
        var offset = await streamSystem.QueryOffset(consumerRef, stream).ConfigureAwait(false);
        return new OffsetTypeOffset(offset);
    }
}).ConfigureAwait(false);
Console.WriteLine("Consumer is running. Press [enter] to exit.");
Console.ReadLine();
await consumer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);
