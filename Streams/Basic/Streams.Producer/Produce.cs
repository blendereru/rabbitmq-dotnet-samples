using System.Net;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;

var streamSystem = await StreamSystem.Create(new StreamSystemConfig()
{
    UserName = "guest",
    Password = "guest",
    Endpoints = new List<EndPoint>() {new IPEndPoint(IPAddress.Loopback, 5552)}
}).ConfigureAwait(false);
const string streamName = "my-stream";
await streamSystem.CreateStream(new StreamSpec(streamName)).ConfigureAwait(false); //creating a stream
var producer = await Producer.Create(new ProducerConfig(streamSystem, streamName)).ConfigureAwait(false);
var message = new Message(Encoding.UTF8.GetBytes("Hello, this is my message"))
{
    ApplicationProperties = new ApplicationProperties()
    {
        { "userName", "blendereru" },
        { "email", "sanzar30062000@gmail.com" }
    }
};

await producer.Send(message).ConfigureAwait(false);
await producer.Close().ConfigureAwait(false);
await streamSystem.Close().ConfigureAwait(false);