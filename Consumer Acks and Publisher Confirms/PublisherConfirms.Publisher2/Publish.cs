using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost", Port = 5672 };
await using var connection = await factory.CreateConnectionAsync();
var channelOptions =
    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
await using var channel = await connection.CreateChannelAsync(channelOptions);

await channel.QueueDeclareAsync("my_queue", durable: true, exclusive: false, autoDelete: false);

var outstandingConfirms = new LinkedList<ulong>();
var semaphore = new SemaphoreSlim(1, 1);
var allMessagesConfirmedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
int confirmedCount = 0;

channel.BasicAcksAsync += async (sender, ea) => await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
channel.BasicNacksAsync += async (sender, ea) =>
{
    Console.Error.WriteLine($"{DateTime.Now} [NACK] Message with delivery tag {ea.DeliveryTag} was negatively acknowledged.");
    await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
};
channel.BasicReturnAsync += (sender, ea) =>
{
    Console.Error.WriteLine($"{DateTime.Now} [RETURN] Message returned: {Encoding.UTF8.GetString(ea.Body.ToArray())}");
    return Task.CompletedTask;
};

for (int i = 0; i < 100; i++)
{
    var properties = new BasicProperties { Persistent = true };
    var seqNo = await channel.GetNextPublishSequenceNumberAsync();
    await semaphore.WaitAsync();
    try
    {
        outstandingConfirms.AddLast(seqNo);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{DateTime.Now} [ERROR] Exception during publish: {ex}");
    }
    finally
    {
        semaphore.Release();
    }
    var body = Encoding.UTF8.GetBytes($"Hello, this is message #{i}");
    await channel.BasicPublishAsync(string.Empty, "my_queue", true, properties, body);
}

await allMessagesConfirmedTcs.Task;
Console.WriteLine("All messages have been confirmed.");

async Task CleanOutstandingConfirms(ulong deliveryTag, bool multiple)
{
    Console.WriteLine($"{DateTime.Now} confirming message: {deliveryTag}, multiple: {multiple}");
    await semaphore.WaitAsync();
    try
    {
        if (multiple)
        {
            var node = outstandingConfirms.First;
            while (node != null && node.Value <= deliveryTag)
            {
                var next = node.Next;
                outstandingConfirms.Remove(node);
                confirmedCount++;
                node = next;
            }
        }
        else
        {
            if (outstandingConfirms.Remove(deliveryTag))
            {
                confirmedCount++;
            }
        }

        if (outstandingConfirms.Count == 0 || confirmedCount == 100)
        {
            allMessagesConfirmedTcs.TrySetResult(true);
        }
    }
    finally
    {
        semaphore.Release();
    }
}
