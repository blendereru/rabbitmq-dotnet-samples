using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync("temp.queue", durable: true, exclusive: false, autoDelete: false);
for (var i = 0; i < 3; i++)
{
    var message = $"Message #{i + 1}";
    var body = Encoding.UTF8.GetBytes(message);
    var props = new BasicProperties()
    {
        MessageId = i.ToString()
    };
    Console.WriteLine($"The message {message} was sent to temp.queue");
    await channel.BasicPublishAsync(string.Empty, "temp.queue", mandatory: true, props, body);
}
