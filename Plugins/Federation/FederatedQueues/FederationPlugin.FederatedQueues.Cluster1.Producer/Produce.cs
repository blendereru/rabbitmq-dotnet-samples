using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync("federated.orders", durable: true, exclusive: false,autoDelete: false);
var message = "Order #12345";
var body = Encoding.UTF8.GetBytes(message);
await channel.BasicPublishAsync(string.Empty, "federated.orders", body);
Console.WriteLine($"Sent {message}");
// var consumer = new AsyncEventingBasicConsumer(channel);
// consumer.ReceivedAsync += async (model, ea) =>
// {
//     var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
//     Console.WriteLine($"Message {msg} was received");
//     await Task.CompletedTask;
// };
// await channel.BasicConsumeAsync("federated.orders", autoAck: true, consumer);
// Console.WriteLine("Waiting for messages. Press [enter] to exit");
// Console.ReadLine();