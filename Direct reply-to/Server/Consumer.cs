using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Consumer
{
    public static async Task Main()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queueName = "rpc_queue";
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[x] Received message: {message}");

            var responseMessage = $"Message consumed: {message}";
            var responseBytes = Encoding.UTF8.GetBytes(responseMessage);

            var props = ea.BasicProperties;
            var replyProps = new BasicProperties
            {
                CorrelationId = props.CorrelationId
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: props.ReplyTo,
                mandatory: true,
                basicProperties: replyProps,
                body: responseBytes
            );
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            Console.WriteLine($"[x] Acknowledged message with delivery tag: {ea.DeliveryTag}");
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
        Console.WriteLine($"Waiting for messages in queue '{queueName}'. Press [enter] to exit.");
        Console.ReadLine();
    }
}
