using System.Text;
using System.Text.Json;
using QuorumQueues.Publisher;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() { HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync("user.create.dlx", ExchangeType.Direct, durable: true, autoDelete: false);
var arguments = new Dictionary<string, object?>()
{
    { "x-queue-type", "quorum" },
    {"x-dead-letter-exchange", "user.create.dlx"},
    {"x-dead-letter-routing-key", string.Empty},
    {"x-delivery-limit", 1},
    {"x-quorum-initial-group-size", 3}
};
await channel.QueueDeclareAsync("user.create", durable: true, exclusive: false, autoDelete: false, arguments);
await channel.QueueDeclareAsync("user.create.dlx.queue", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("user.create.dlx.queue", "user.create.dlx", string.Empty);
var user = new User()
{
    UserName = "blendereru",
    Password = "Qwerty123+"
};
var userJson = JsonSerializer.Serialize(user);
var userBody = Encoding.UTF8.GetBytes(userJson);
var props = new BasicProperties()
{
    Persistent = true,
    Expiration = "60000"
};
Console.WriteLine($"Publishing the message for user creation: {user.UserName}");
await channel.BasicPublishAsync(string.Empty, "user.create", mandatory: false, props, userBody);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received message: {message}");
    if (ea.BasicProperties.Headers != null &&
        ea.BasicProperties.Headers.TryGetValue("x-death", out var deathHeader) &&
        deathHeader is IList<object> xDeathList && xDeathList.Count > 0)
    {
        var xDeath = xDeathList[0] as IDictionary<string, object>;
        if (xDeath != null && xDeath.TryGetValue("reason", out var reasonObj))
        {
            var reason = Encoding.UTF8.GetString((byte[])reasonObj);
            if (reason == "expired")
            {
                Console.WriteLine("This message was dead-lettered due to expiration.");
            }
            else
            {
                Console.WriteLine($"This message was dead-lettered due to: {reason}");
            }
        }
    }
    await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
};
await channel.BasicConsumeAsync("user.create.dlx.queue", autoAck: false, consumer);
Console.WriteLine("Waiting for dead-lettered messages. Press [enter] to exit.");
Console.ReadLine();