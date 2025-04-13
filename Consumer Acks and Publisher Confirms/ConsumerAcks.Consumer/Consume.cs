using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
    if (!string.IsNullOrEmpty(ea.BasicProperties.MessageId))
    {
        var messageId = ea.BasicProperties.MessageId!;
        if (messageId == "2")
        {
            Console.WriteLine($"Nack-ing message: {body}");
            await currentChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            //or use BasicRejectAsync if not planning to nack multiple messages
            //await currentChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
        else
        {
            Console.WriteLine($"Ack-ing message: {body}");
            await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
    }
};
await channel.BasicConsumeAsync("temp.queue", autoAck: false, consumer);