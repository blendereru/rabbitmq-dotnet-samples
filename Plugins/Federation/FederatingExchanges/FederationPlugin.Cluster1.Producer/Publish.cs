using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672}; //upstream
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync("federated.logs", ExchangeType.Fanout);
await channel.QueueDeclareAsync("federated.logs.queue", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("federated.logs.queue", "federated.logs", routingKey: "my-rk");
var body = Encoding.UTF8.GetBytes("Test message");
await channel.BasicPublishAsync("federated.logs", "my-rk", mandatory: true, body);
Console.WriteLine("Published Test message");