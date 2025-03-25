using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672};
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();
var firstQueue = await channel.QueueDeclareAsync(queue: "first_client_queue", exclusive: false);
var secondQueue = await channel.QueueDeclareAsync(queue: "second-client-queue", exclusive: false);
await channel.ExchangeDeclareAsync("local_random_exchange", "x-local-random");
await channel.QueueBindAsync(firstQueue.QueueName, exchange: "local_random_exchange", routingKey: string.Empty);
await channel.QueueBindAsync(secondQueue.QueueName, exchange: "local_random_exchange", routingKey: string.Empty);
var replyQueueName = "amq.rabbitmq.reply-to";
var consumer = new AsyncEventingBasicConsumer(channel);
var correlationId = Guid.NewGuid().ToString();
var props = new BasicProperties()
{
    CorrelationId = correlationId,
    ReplyTo = replyQueueName
};
var tcs = new TaskCompletionSource<string>();
consumer.ReceivedAsync += async (model, ea) =>
{
    if (ea.BasicProperties.CorrelationId == correlationId)
    {
        var response = Encoding.UTF8.GetString(ea.Body.ToArray());
        tcs.SetResult(response);
    }
};
await channel.BasicConsumeAsync(queue: replyQueueName, autoAck: true, consumer);
for (int i = 0; i < 100; i++)
{
    var message = $"Hello, this is message #{i}!";
    var body = Encoding.UTF8.GetBytes(message);
    await channel.BasicPublishAsync(exchange: "local_random_exchange", routingKey: string.Empty, basicProperties: props, body: body, mandatory: false);
    Console.WriteLine($"Sent request: {message}");
}
var responseMessage = await tcs.Task;
Console.WriteLine($"Received response: {responseMessage}");
Console.ReadLine();