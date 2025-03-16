using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Publisher
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> CallbackMapper = new();

    public static async Task Main(string[] args)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var replyToQueueName = "amq.rabbitmq.reply-to";

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            if (CallbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
            {
                tcs.SetResult(ea.Body.ToArray());
            }
            await Task.Yield();
        };

        await channel.BasicConsumeAsync(
            queue: replyToQueueName,
            autoAck: true,
            consumer: consumer
        );

        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyToQueueName
        };
        var message = "Hello, this is blendereru";
        var body = Encoding.UTF8.GetBytes(message);
        var tcs = new TaskCompletionSource<byte[]>();
        CallbackMapper[correlationId] = tcs;
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey:"rpc_queue", true, basicProperties: props, body: body);
        var responseBytes = await tcs.Task;
        var responseMessage = Encoding.UTF8.GetString(responseBytes);
        Console.WriteLine($"Received response: {responseMessage}");
        Console.ReadLine();
    }
}