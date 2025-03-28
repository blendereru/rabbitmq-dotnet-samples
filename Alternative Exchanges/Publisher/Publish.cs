using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
await using var connection = await connectionFactory.CreateConnectionAsync(); 
await using var channel = await connection.CreateChannelAsync();
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
var text = "Hello, this is a routed message";
var body = Encoding.UTF8.GetBytes(text);
await channel.BasicPublishAsync("direct-exchange", "key1", mandatory: true, body);
Console.WriteLine($"Publishing the message: {text}");
var anotherText = "Hello, this is a unrouted message";
var anotherBody = Encoding.UTF8.GetBytes(anotherText);
await channel.BasicPublishAsync("direct-exchange", "key2", anotherBody);
Console.WriteLine($"Publishing the message: {anotherText}");