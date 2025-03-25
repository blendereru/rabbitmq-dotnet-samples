using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var queueName = "first_client_queue";
var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var queue = await channel.QueueDeclareAsync(queueName, exclusive: false);
await channel.QueueBindAsync(queue.QueueName, exchange: "local_random_exchange", routingKey: string.Empty);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"[x] Received message: {message}");

    var responseMessage = $"Message consumed: {message}";
    var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
    var correlationId = ea.BasicProperties.CorrelationId!;
    var props = new BasicProperties()
    {
        CorrelationId = correlationId
    };
    var chn = ((AsyncEventingBasicConsumer)model).Channel; 
    await chn.BasicPublishAsync(exchange: string.Empty, routingKey: ea.BasicProperties.ReplyTo!,
        basicProperties: props, body: responseBytes, mandatory: true);
};
await channel.BasicConsumeAsync(queue: queueName, consumer: consumer, autoAck: true);
Console.WriteLine($"Waiting for messages in queue '{queueName}'. Press [enter] to exit.");
Console.ReadLine();