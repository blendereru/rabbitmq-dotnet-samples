using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, "invoices")
{
    IsSuperStream = true,
    Reference = "my-superstream-sac-consumer",
    IsSingleActiveConsumer = true,
    OffsetSpec = new OffsetTypeFirst(),
    MessageHandler = async (stream, consumerSource, context, message) =>
    {
        var body = Encoding.UTF8.GetString(message.Data.Contents);
        Console.WriteLine($"Received message id: {message.Properties.MessageId}, body: {body}, stream: {stream}, offset: {context.Offset}");
        await consumerSource.StoreOffset(context.Offset).ConfigureAwait(false);
        await Task.CompletedTask.ConfigureAwait(false);
    },
    ConsumerUpdateListener = async (consumerRef, stream, isActive) =>
    {
        ulong offset = 0;
        try
        {
            offset = await streamSystem.QueryOffset(consumerRef, stream).ConfigureAwait(false);
        }
        catch (OffsetNotFoundException)
        {
            Console.WriteLine($"Offset not found for stream {stream} and consumer {consumerRef}. Will use the first offset");
            return new OffsetTypeFirst();
        }
        await Task.CompletedTask.ConfigureAwait(false);
        return new OffsetTypeOffset(offset + 1);
    }
}).ConfigureAwait(false);
Console.WriteLine("Consumer is running. Press [enter] to exit.");
Console.ReadLine();
await consumer.Close().ConfigureAwait(false);
await consumer.Close().ConfigureAwait(false);