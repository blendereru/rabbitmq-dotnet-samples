using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.ExchangeDeclareAsync("dead-letter-exchange", ExchangeType.Direct);
await channel.QueueDeclareAsync("dead-letter-queue", exclusive: false, autoDelete: false);
await channel.QueueBindAsync("dead-letter-queue", "dead-letter-exchange", string.Empty);
var arguments = new Dictionary<string, object?>()
{
    { "x-dead-letter-exchange", "dead-letter-exchange" },
    { "x-dead-letter-routing-key", string.Empty},
    {"x-overflow", "reject-publish-dlx"},
    {"x-max-length", 1 }
};
await channel.QueueDeclareAsync("my-queue", autoDelete: false, exclusive: false, arguments: arguments);
var body1 = Encoding.UTF8.GetBytes("Hello, this is a good message");
var body2 = Encoding.UTF8.GetBytes("Hello, this is the message for dead-letter exchange");
await channel.BasicPublishAsync(string.Empty, "my-queue",  body1);
await channel.BasicPublishAsync(string.Empty, "my-queue", body2);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received dead-lettered message: {message}");
    if (ea.BasicProperties.Headers?.TryGetValue("x-death", out var deathHeader) == true)
    {
        var deathInfo = deathHeader as List<object>;
        if (deathInfo != null)
        {
            foreach (var entry in deathInfo)
            {
                if (entry is Dictionary<string, object> deathEntry)
                {
                    var reason = deathEntry.TryGetValue("reason", out var reasonVal) 
                        ? Encoding.UTF8.GetString((byte[])reasonVal) 
                        : "unknown";
                
                    var queue = deathEntry.TryGetValue("queue", out var queueVal) 
                        ? Encoding.UTF8.GetString((byte[])queueVal) 
                        : "unknown";
                
                    var count = deathEntry.TryGetValue("count", out var countVal) 
                        ? Convert.ToInt64(countVal) 
                        : 0;
                    var time = deathEntry.TryGetValue("time", out var timeVal) 
                        ? (timeVal is AmqpTimestamp timestamp 
                            ? DateTimeOffset.FromUnixTimeSeconds(timestamp.UnixTime).DateTime 
                            : (DateTime)timeVal) 
                        : DateTime.MinValue;

                    Console.WriteLine($"Dead-letter reason: {reason}");
                    Console.WriteLine($"Origin queue: {queue}");
                    Console.WriteLine($"Death count: {count}");
                    Console.WriteLine($"First death time: {time:yyyy-MM-dd HH:mm:ss}");
                }
            }
        }
    }
};
await channel.BasicConsumeAsync("dead-letter-queue", autoAck: true, consumer);
Console.WriteLine("Waiting for dead-lettered messages. Press [enter] to exit.");
Console.ReadLine();