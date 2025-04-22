using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
for (int i = 0; i < 100; i++)
{
    var body = Encoding.UTF8.GetBytes($"Message #{i}");
    await channel.BasicPublishAsync(string.Empty, "shovel.src.queue", body);
}
Console.WriteLine("Published 100 messages to src.queue");