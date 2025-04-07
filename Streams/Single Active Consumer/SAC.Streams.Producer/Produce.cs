using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
await streamSystem.CreateStream(new StreamSpec("my-sac-stream")).ConfigureAwait(false);
var producer = await Producer.Create(new ProducerConfig(streamSystem, "my-sac-stream")).ConfigureAwait(false);
var messages = new List<Message>();
for (int i = 0; i < 50; i++)
{
    var body = Encoding.UTF8.GetBytes($"Message #{i}");
    var message = new Message(body);
    await producer.Send(message).ConfigureAwait(false);
}
Console.WriteLine("Sending 50 messages to my-sac-stream");
await producer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);