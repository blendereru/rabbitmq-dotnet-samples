using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5673}; // downstream
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync("federated.logs", ExchangeType.Fanout);
await channel.QueueDeclareAsync("federated.logs.another.queue", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("federated.logs.another.queue", "federated.logs", routingKey: "my-rk");
var body = Encoding.UTF8.GetBytes("Test message");
await channel.BasicPublishAsync("federated.logs", "my-rk", mandatory: true, body);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"Message '{message}' was received");
    await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
};
Console.WriteLine("Consuming from federated.logs.another.queue on green node");
await channel.BasicConsumeAsync("federated.logs.another.queue", autoAck: false, consumer);