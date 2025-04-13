using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync("my_queue", durable: true, exclusive: false, noWait: false, autoDelete: false);
await channel.TxSelectAsync();
try
{
    for (int i = 0; i < 100; i++)
    {
        var body = Encoding.UTF8.GetBytes($"Hello, this is message #{i}");
        await channel.BasicPublishAsync(string.Empty, "my_queue", body);
    }
    Console.WriteLine("Commiting transaction for 100 messages");
    await channel.TxCommitAsync();
    Console.WriteLine("Transaction was successfully committed");
}
catch (Exception ex)
{
    await channel.TxRollbackAsync();
    Console.WriteLine($"Transaction rolled back due to an error: {ex.Message}");
}