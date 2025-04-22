using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5673};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"Received {message}");
    await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: true);
};
await channel.BasicConsumeAsync("shovel.dest.queue", autoAck: false, consumer);
Console.WriteLine("Waiting for messages in shovel.dest.queue. Press [enter] to exit");
Console.ReadLine();