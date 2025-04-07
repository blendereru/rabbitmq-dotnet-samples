using System.Buffers;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Streams.Consumer;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
const string streamName = "my-stream";
var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, streamName)
{
    Crc32 = new UserCrc32(),
    Reference = "first_consumer",
    OffsetSpec = new OffsetTypeFirst(),
    MessageHandler = async (_, consumer, context, message) =>
    {
        var body = Encoding.UTF8.GetString(message.Data.Contents.ToArray());
        var userName = message.ApplicationProperties["userName"];
        var email = message.ApplicationProperties["email"];
        if (userName == null || email == null)
        {
            Console.WriteLine("The message doesn't contain the user's metadata");
            await Task.CompletedTask.ConfigureAwait(false);
        }
        else
        {
            await consumer.StoreOffset(context.Offset).ConfigureAwait(false);
            Console.WriteLine($"The message {body} was received from {userName} with email: {email}");
        }
    }
}).ConfigureAwait(false);
Console.WriteLine("Consumer is running. Press [enter] to exit.");
Console.ReadLine(); // is needed when crc is set as hashing takes time 
await consumer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);