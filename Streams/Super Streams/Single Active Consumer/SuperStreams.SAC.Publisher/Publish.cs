using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()).ConfigureAwait(false);
var producer = await Producer.Create(new ProducerConfig(streamSystem, "invoices")
{
    SuperStreamConfig = new SuperStreamConfig()
    {
        Routing = msg => msg.Properties.MessageId.ToString(),
    }
}).ConfigureAwait(false);
for (int i = 0; i < 100; i++)
{
    var message = new Message(Encoding.UTF8.GetBytes($"Hello, this is message #{i}"))
    {
        Properties = new Properties() { MessageId = $"id_{i}" },
        ApplicationProperties = new ApplicationProperties()
        {
            {"Id", i}
        }
    };
    await producer.Send(message).ConfigureAwait(false);
}
Console.WriteLine("Sending 100 messages to invoices");
await producer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);