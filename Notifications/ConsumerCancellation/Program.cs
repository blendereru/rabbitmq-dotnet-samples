using System.Text;
using ConsumerCancellation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory()
{
    HostName = "localhost",
    Port = 5672,
    ClientProperties = new Dictionary<string, object?>
    {
        ["capabilities"] = new Dictionary<string, object>
        {
            ["consumer_cancel_notify"] = true
        }
    }
};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var consumer = new CancellableConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"Message {message} was received");
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: true);
};
await channel.BasicConsumeAsync("some.queue", autoAck: false, consumer);
Console.WriteLine("Waiting for messages in some.queue");
Console.ReadLine();

