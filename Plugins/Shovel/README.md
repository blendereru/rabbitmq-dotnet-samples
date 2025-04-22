## Shovel Plugin
Shovel plugin is needed to automatically move the messages from the `source`(exchange or queue) to the
`destination`(also an exchange or queue). So it is possible to set the binding between queue and exchange or vice versa.

## How does it internally work ?
Shovel internally launches a consumer that connects to the `source` cluster, consumes the messages published
to `source` and `republishes` them to the `destination`. Shovel can use a confirmation on both
ends to ensure message safety.

## Declaration
Here, I use dynamic shovel for the example, but it is possible to use static shovel as well. The difference
is that `static shovels` require a definition in a configuration file, node restart and pre-built 
topology. Dynamic shovels can be defined through `runtime parameters`, `management UI`,  or `HTTP API`.
I will use runtime parameters for declaration, you just need to know the important `keys` that are available:
* `reconnect-delay` - The duration in seconds to wait before applying reconnection after being disconnected at either end.
* `ack-mode` - How shovel should acknowledge the consumed messages. Default is `on-confirm`, meaning that
messages are `ack`-ed to `source` after they have been confirmed by destination. If set to `on-publish`,
acks to `source` after the messages were published to the `destination`. If set to `no-ack`, messages
are `auto-acked`. That is the fastest option.
* `src-uri` - `dest-uri` - mandatory source and destination connection uris. The value can be a single string(uri), or a list of strings,
in which case the shovel will randomly pick one URI from the list until one of the endpoints succeeds. 
Dynamic shovels are automatically defined on all nodes of the hosting cluster on which the shovel plugin is enabled.
* `src-protocol` - `dest-protocol` - protocols to use when connecting to the source or destination.
Either amqp091 or amqp10. If omitted it will default to amqp091. Shovel can move messages between different
amqp protocol-ed brokers.
* `src-queue` - `src-exchange` - either(not both) of these values must be set. Defines the source to consume
from. In case of `src-exchange` the shovel will declare an `exclusive` queue and bind it to the named exchange with `src-exchange-key` before consuming from the queue.
* `src-exchange-key` - Routing key when using `src-exchange`
* `src-consumer-args` - consumer arguments to specify for the `source`. For example, `x-single-active-consumer`
* `src-prefetch-count` - The maximum number of unacknowledged messages copied over a shovel at any one time.
For example, if `src-prefetch-count` is set to 50, and 100 messages arrived to the `source`, shovel consumes
50 messages, sends them to `dest`, and depending on the `ack-mode` (e.g., on-confirm),
the shovel waits for confirmations from the destination before acknowledging messages to the `source`.
* `src-delete-after` - Defines, when the shovel should deletes itself(default is never). If set to `queue-length`,
instructs the shovel to measure the number of messages in the source queue at the time of its startup
and to delete itself automatically after transferring that exact number of messages. If set to `integer`,
shovel deletes itself after processing the number of messages specified in the value.
* `dest-queue` - `dest-exchange` - either(not both) of these values must be set. Defines the destination
to republish(move) the messages.
* `dest-exchange` - The exchange to which messages should be published.If the destination exchange does
not exist on the destination broker, it will not be declared; the shovel will fail to start. This rule
applies to `src-exchange` as well.

## Pre-configured topology
RabbitMQ can pre-define both the `source` and `destination` if they are not declared yet. This works for
`dynamic` shovels only. By default, RabbitMQ creates a `durable` queue if the `src-queue` or `dest-queue`
are not defined, and their `src-queue-args` and `dest-queue-args` are not specified. If they are, it will 
create a `source` or `destination` with these values(e.g. if `queue-type` is `quorum` will create quorum queue, etc.)

However, we can prevent automatic declaration and force the shovel to wait until messages are declared by
user. In order to do this, we can:
1) Wait until the pre-declared topology is available using `rabbitmq.conf`:
```config
# all shovels started on this node will use pre-declared topology
shovel.topology.predeclared = true
```

2) If specific shovels need pre-declaration, we can use the following keys:
* `src-predeclared` - When set to true, the plugin waits until src-queue is available instead of declaring the topology itself using src-queue-args.
* `dest-predeclared` - When set to true, the plugin waits until dest-queue is available instead of declaring the topology itself using dest-queue-args.

## Example configuration
In the compose file, I specified 2 nodes, and the `rabbit-a1` node's messages will be moved to `rabbit-a2`
node's queue. In both nodes, the shovel plugin must be enabled(along with shovel_management if need to create a shovel
through `management UI`):
```bash
rabbitmq-plugins enable rabbitmq_shovel rabbitmq_shovel_management
```
After this, a `shovel` can be declared. For example, in `Client1`, I specified `shovel.src.queue` to
publish the messages, so it will act as a `source`. Therefore, `Client2` serves as a `destination`.
So, any sort of configuration can be created for shovelling both queues. Just for an example, consider
the following conf:
```bash
docker exec rabbitmq-a1 rabbitmqctl set_parameter shovel my-shovel '{"src-uri": "amqp://", "src-queue": "shovel.src.queue", "dest-uri": "amqp://guest:guest#rabbit-a2/%2F", "dest-queue": "shovel.dest.queue", "ack-mode": "on-confirm", "prefetch-count": 50}'
```
Here, we are creating the most basic shovel by applying `src-prefetch-count`. But for convenience, it is
better to use `management UI` for that purpose. Something to be careful of, is the `uri`s to specify

## Useful Links
* [Dynamic shovel](https://www.rabbitmq.com/docs/shovel-dynamic)
* [Monitor shovel](https://www.rabbitmq.com/docs/shovel#status)
* [RabbitMQ Uri Spec](https://www.rabbitmq.com/docs/uri-spec)