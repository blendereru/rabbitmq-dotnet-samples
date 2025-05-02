using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerCancellation;

public class CancellableConsumer : AsyncEventingBasicConsumer
{
    public CancellableConsumer(IChannel channel) : base(channel) {}
    public override async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = new CancellationToken())
    {
        Console.WriteLine($"Consumer '{consumerTag}' has been cancelled unexpectedly.");
        await Task.CompletedTask;
    }
}