## Consumer acknowledgements and Publisher Confirms
[Consumer acknowledgements and Publisher confirms](https://www.rabbitmq.com/docs/confirms) are essential
to ensure that messages were successfully delivered and consumed. As their names state, consumer acks
are needed to mark the message as `acknowledged`, thus being free to be deleted. Publisher confirms
work on server side, to ensure that the broker successfully delivered the message to specific queue.

## Consumer acknowledgements
Consumers, by default(automatic acknowledgement mode), acknowledge the message as soon as they sent. This behaviour is not safe
as if consumers' TCP connection or channel is closed before successful delivery, the message sent by the
server will be lost. So, RabbitMQ provides manual acknowledgement mode to ensure the successful processing:
```csharp
consumer.ReceivedAsync += async (model, ea) =>
{
    var currentChannel = ((AsyncEventingBasicConsumer)model).Channel;
    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
    if (!string.IsNullOrEmpty(ea.BasicProperties.MessageId))
    {
        var messageId = ea.BasicProperties.MessageId!;
        if (messageId == "2")
        {
            Console.WriteLine($"Nack-ing message: {body}");
            await currentChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            //or use BasicRejectAsync if not planning to nack multiple messages
            //await currentChannel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
        else
        {
            Console.WriteLine($"Ack-ing message: {body}");
            await currentChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
    }
};
await channel.BasicConsumeAsync("temp.queue", autoAck: false, consumer); // note the autoAck parameter
```
In this case, if the `MessageId` is 2 we negatively acknowledge the message, otherwise, positively.
In a manual acknowledgment node, we can mark the message as `ack`-ed, `nack`-ed, and `reject`-ed. All 
methods take `deliveryTag` as the first parameter. These [delivery tags](https://www.rabbitmq.com/docs/confirms#consumer-acks-delivery-tags) are needed to uniquely identify the delivery
on the channel.
* `basic.ack` - specifies that the message was successfully handled by the consumer.
* `basic.reject` - specifies that the message is negatively acknowledged. `Requeue` parameter is needed
to requeue the message to the original queue upon rejection.
* `basic.nack` - specifies that the message is negatively acknowledges, but has `multiple` boolean parameter.

`Multiple` boolean parameter is present in `basic.ack` and `basic.nack` methods only. I will use [documentation](https://www.rabbitmq.com/docs/confirms#consumer-acks-multiple-parameter)
to explain the purpose of it:
> When the multiple field is set to true, RabbitMQ will acknowledge all outstanding delivery tags up to and including the tag specified in the acknowledgement. Like everything else related to acknowledgements, this is scoped per channel. For example, given that there are delivery tags 5, 6, 7, and 8 unacknowledged on channel Ch, when an acknowledgement frame arrives on that channel with delivery_tag set to 8 and multiple set to true, all tags from 5 to 8 will be acknowledged. If multiple was set to false, deliveries 5, 6, and 7 would still be unacknowledged.

`basic.reject` historically didn't have the `multiple` fields, so `basic.nack` was later introduced.
In other directions, their behaviours is the same.

## Publisher confirms
There are 2 ways to enable publisher confirms in RabbitMQ. First way is using transactional channel, 
that ensures that a set of message operations are treated as single unit:
```csharp
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
```
In this way, if the exception is thrown in a mid-way of publishing, the entire transaction is rolled back.
But, why not to enable publishing confirms for `each` message ? This is one of the drawbacks of 
transactions. But for this case RabbitMQ introduced a `confirmation` mechanism. This works pretty much the
same as consumers acks. Whenever a broker receives a message, it sends `basic.ack` to the client. The 
client can then implement the `events` of specific broker response:
* `channel.BasicAcksAsync` - confirms messages as it handles them.
* `channel.BasicNacksAsync` - when the broker is unable to handle messages successfully.
* `channel.BasicReturnAsync` - if the message is published as `mandatory` and it couldn't be routed to any queue.

The first two events contain `DeliveryTag` and `Multiple` fields. `Multiple` field indicates that all
messages up to and including the one with the sequence number have been `ack`-ed(or `nack`-ed).
Definition:
```csharp
var channelOptions =
    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
await using var channel = await connection.CreateChannelAsync(channelOptions);
```
The configuration above, makes the channel in `confirm` mode. Internally, client sends the `confirm.select` method,
and broker responds with `confirm.select-ok`. This marks the channel as being in `confirm` mode, and all subsequent
messages sent through this channel, will have a `unique` sequence number(note `publisherConfirmationTrackingEnabled` parameter).
In this way, each message will have a `PublishSequenceNumberHeader` to each published message,
containing the message's unique sequence number. Implementing the events:
```csharp
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
```
upon receiving positive or negative acknowledgement for message being confirmed by the broker, we call
`CleanOutstandingConfirms` method, which tracks the message by their sequence number using linked list
and deletes the messages that were nack-ed or ack-ed. The sequence number of message can be obtained 
before publishing, using `GetNextPublishSequenceNumberAsync` method:
```csharp
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
```
Then, after the message arrived from specific event, we compare the `deliveryTag` with the current node's 
value, and if they match, delete the message. Delivery tag is the same sequence number but `can't` be 
obtained before publishing.