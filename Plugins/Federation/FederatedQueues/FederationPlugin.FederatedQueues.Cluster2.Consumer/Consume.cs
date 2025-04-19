using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5673};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync("federated.orders", durable: true, exclusive: false,autoDelete: false);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.Default.GetString(ea.Body.ToArray());
    Console.WriteLine($"Message {message} was received");
    await Task.CompletedTask;
};
await channel.BasicConsumeAsync("federated.orders", autoAck: true, consumer);
Console.WriteLine("Waiting for messages. Press [enter] to exit");
Console.ReadLine();