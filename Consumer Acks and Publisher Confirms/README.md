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
