## Dead-Letter Exchanges
Dead-lettering mechanism in RabbitMQ is needed to not to lose messages that some queue couldn't process. The messages are 
dead-lettered in the following cases(from [documentation](https://www.rabbitmq.com/docs/dlx#overview)):
* The message is negatively acknowledged by an AMQP 1.0 receiver using the rejected outcome or by an AMQP 0.9.1 consumer using basic.reject or basic.nack with requeue parameter set to false, or
* The message expires due to per-message TTL, or
* The message is dropped because its queue exceeded a length limit, or
* The message is returned more times to a quorum queue than the delivery-limit.

## Example setup
In the example, we publish 2 messages to the `my-queue` queue which will serve as the main queue. We set the following arguments
to the queue:
1) `{ "x-dead-letter-exchange", "dead-letter-exchange" }` -  sets the `dead-letter-exchange` as the exchange
to which the dead-lettered messages will be published.
2) `{ "x-dead-letter-routing-key", string.Empty}` - the routing key value the dead-lettered message will be published with.
If we didn't specify this value, the message would be published to the `dead-letter-exchange` with the routing key value being
`my-queue`(we published to the original queue in this way), as we don't have the queue bound to the exchange with this routing key value, the message would be silently dropped.
This is the [default](https://www.rabbitmq.com/docs/dlx#routing) behaviour
3) `{"x-overflow", "reject-publish-dlx"}` - how should queue behave upon reaching the maximum queue length. We set to 
publish the rejected messages to the dead letter exchange. The default value is [drop-head](https://www.rabbitmq.com/docs/maxlength#overflow-behaviour)
4) `{"x-max-length", 1 }` - the maximum queue length of 1 message. If the queue receive more messages, they are dead-lettered.

The `Producer` publishes 2 messages to the `my-queue` and consumes the messages from the `dead-letter-queue`. This queue
is bound to `dead-letter-exchange` on the routing key ''(empty). We publish 2 messages and expect the second message to
be dead-lettered and upon receiving inspect its header `x-death`(array of dictionaries). The [documentation](https://www.rabbitmq.com/docs/dlx#effects) 
provides a list of the header's value and in the example we try to retrieve the following keys:
1) reason - name describing why the message was dead-lettered and is one of the following: 
    * rejected: the message was rejected
    * expired: the message TTL has expired
    * maxlen: the maximum allowed queue length was exceeded
    * delivery_limit: the message is returned more times than the limit (set by policy argument delivery-limit of quorum queues).
2) queue - The name of the queue this message was dead lettered from.
3) count - How many times this message was dead lettered from this queue for this reason.
4) time - When this message was dead lettered the first time from this queue for this reason.

The `x-death` header contains much more interesting headers than that, like x-first-death-reason, x-last-death-reason. 
You can read about them in the documentation. 

The `Consumer` part is responsible for consuming from the `my-queue` and doesn't do anything except for retrieving the message's body.
