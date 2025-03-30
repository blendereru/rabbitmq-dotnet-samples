using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received message: {message}");
};
await channel.BasicConsumeAsync("my-queue", autoAck: true, consumer);
Console.WriteLine("Waiting for original messages. Press [enter] to exit.");
Console.ReadLine();