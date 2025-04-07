using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuorumQueues.User_Create.Consumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory() {HostName = "localhost", Port = 5672};
await using var connection = await connectionFactory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
    var user = JsonSerializer.Deserialize<User>(body);
    if (user != null)
    {
        var newChannel = ((AsyncEventingBasicConsumer)model).Channel;
        await using var db = new ApplicationContext();
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName);
        if (existingUser != null)
        {
            Console.WriteLine($"Attempt to create the user with the existing username: {user.UserName}");
            await newChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
        else
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine($"Successfully created user: {user.UserName}");
            await newChannel.BasicAckAsync(ea.DeliveryTag, multiple: true);
        }
    }
};
await channel.BasicConsumeAsync("user.create", autoAck: false, consumer);