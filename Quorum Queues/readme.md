## Quorum Queues
Quorum Queues allows to store a queue in multiple RabbitMQ nodes. In this way, the queue is guaranteed to always be 
available and clients should not worry about the queue being down. It is something like an opposite of transient
queues. Quorum queues can recover from one node being down, as the queue is replicated on other nodes, they can
serve for a client.
## How does it actually work under the hood ?
Quorum queues use [Raft consensus algorithm](https://raft.github.io/) and this introduces a terms called a `leader` and
its `followers`. A leader is the node which appends the log entries and makes sure its followers have replicated the data.
Clients interact with the leader, not with followers. Once a leader becomes down, the followers start a new leader
election process, which is described [here](https://thesecretlivesofdata.com/raft/) and [here](https://raft.github.io/raft.pdf).
I will update the document once i finish reading the pdf, but for now it is enough to understand these terms.

## Quorum Queues vs Classic Queues
One thing that quorum queues can do is [poison message handling](https://www.rabbitmq.com/docs/quorum-queues#poison-message-handling).
The poison messages occur after a consumer couldn't handle the message(possibly because of consumer failure) and makes it requeue it to the source queue
with `x-delivery-count` header incremented each time it was redelivered to the quorum queue. The default value is set
to 20, that is the message redelivery limit is 20 after which it is dropped ot dead-lettered(if has one configured).
This value can be overriden with `x-delivery-limit` optional argument or with [policy(recommended)](https://www.rabbitmq.com/docs/quorum-queues#position-message-handling-configuring-limit).

Quorum queues don't support global [Qos prefetch](https://www.rabbitmq.com/docs/confirms#channel-qos-prefetch). Global Qos prefetch
allows a channel to set a single prefetch limit for all consumers using that channel. This approach is not applicable
with quorum queues as consumers are typically connected to the quorum queue's leader. 

Take a look at [feature matrix](https://www.rabbitmq.com/docs/quorum-queues#feature-matrix) for more differences
between them.

## Dead-lettering
Dead lettering also works differently in quorum queues. Traditionally, in classic queues, we would have dead lettered
the messages that were rejected or nacked via `basic.nack` or `basic.reject` with `requeue` parameter set to `false`.
In quorum queues, such messages are redelivered with `x-delivery-count` header(poisoned). So if the `delivery-limit`
is configured in the queue, in this way the message is dead-lettered. In other ways, the dead lettering works similarly
to the classic queues.

Also, dead-lettering in quorum queues support `at-least-once` dead-lettering. In this way, there is an internal consumer 
that is co-located with on the `leader` node, that is responsible for transferring the messages from source quorum 
queue to its `dead-letter-exchange`. This consumer only manages `dead-lettered` messages by `prefetching` them from the queue(32 messages by default), 
and then republishes to the designated `DLX` with `publisher confirms` enabled, ensuring that each message is acknowledged by the target queue
before being removed from the source queue. Once a message is successfully acknowledged by the DLX, it is removed
from the source quorum queue.

The default behaviour of dead-lettering is `at-most-once`. This term indicates that each message is delivered to the
recipient either once or not at all. In the context of dead lettering, the `at-most-once` strategy implies
that once a message is designated for dead lettering, the system will make a `single` attempt to transfer
it to the `DLX`. If this transfer fails, the system does not retry, and the message may be lost. 

In order to enable `at-least-once` strategy we have to:
* Set `dead-letter-strategy` to `at-least-once`. The default value is `at-most-once`
* Set `overflow` to `reject-publish`. Default is `drop-head`. Note that quorum queues don't support 
`reject-publish-dlx` value due to `poison` message handling mechanism.
* Configure a `dead-letter-exchange`
* Enable `stream_queue` feature flag if not enabled.

> [!NOTE] 
> when setting `overflow` to `drop-head` when enabling `at-least-once` strategy would make it fall back
to `at-most-once` so all settings must be just like above, otherwise the strategy is dismissed.

## Understanding the example
### Publisher
In the example, the publisher serves as a client for publishing a message and consuming from the dead-letter
queue. I declare queue arguments using [optional queue arguments](https://www.rabbitmq.com/docs/queues#optional-arguments),
but it is better to use [policies](https://www.rabbitmq.com/docs/parameters#policies) as the policy
definition can be changed dynamically, while arguments can't:
1) `{ "x-queue-type", "quorum" }` - declaring a queue of type `quorum`.
2) `{"x-dead-letter-exchange", "user.create.dlx"}` - setting dead letter exchange for the quorum queue.
3) `{"x-dead-letter-routing-key", string.Empty}` - routing key used for dead lettered messages.
If not specified, the messages would be routed to `DLX` using the routing key using which the message
was directed to quorum queue itself.
4) `{"x-delivery-limit", 1}` - message can be poisoned(redelivered to quorum queue) only once, after
which it is dead-lettered
5) `{"x-quorum-initial-group-size", 3}` - setting the initial replication factor. This parameter ensures
that when the quorum queue is declared, it will only have 3 replicas, one on each node in the cluster.
If the number of nodes is less than this parameter's value, the value is set as the number of cluster nodes.

Also, the message was published with `ttl` value of 60 seconds(1min), so that when the message was dead-lettered
we could observe the dead-lettering reason. When consuming from the dead lettered exchange's queue(we will
call it target queue), we enable manual `ack` mode, as in quorum queues we must ensure that the messages
aren't lost.
### Consumer
The consumer saves the user's data in `MSSQL` database, and for the case if the user already exists
in the db, rejects the message. Rejecting the message for the first time results in message being
`poisoned`, and rejecting it for the second time results in dead-lettering, as we have a limit set in a queue.

## What else ?
There a bunch of interesting parameters that we could play with when using quorum queues. And one of them
is [manual replica management](https://www.rabbitmq.com/docs/quorum-queues#replica-management). In the compose file,
i use 5 nodes to form a cluster, but in the queue I use only 3 of them. When new nodes were added to the
cluster, we could increase the replicas number up to the newly added nodes:
```bash
rabbitmq-queues add_member -p / user.create rabbit@rabbit4
```
Using the command above, we add `user.create` quorum queue's replica to one more node(in my case 4th).
In this case, we don't need rerun the node or change the code, the changes are automatically triggered.

Another configuration is setting `queue-leader-locator`(`x-queue-leader-locator` when using optional arguments)
which is responsible for choosing the nod in which the leader will live, that can take 2 values:
* `client-local`: Pick the node the client that declares the queue is connected to. This is the default value.
* `balanced`: If there are overall less than 1000 queues (classic queues, quorum queues, and streams), pick the
node hosting the minimum number of quorum queue leaders. If there are overall more than 1000 queues, pick a random node.

When we have a bunch of quorum queues and have an issue when all the leaders of these quorum queues co-locate
in one node, we can [rebalance the replicas](https://www.rabbitmq.com/docs/quorum-queues#replica-rebalancing),
so that clients interacted with other nodes thus balancing the load:
```bash
# rebalances a subset of quorum queues
rabbitmq-queues rebalance quorum --queue-pattern "orders.*"
```
In this case, we try to rebalance the quorum queues start with `orders`.

Also, it is important to read about [Continuous Membership Reconcilation](https://www.rabbitmq.com/docs/quorum-queues#replica-reconciliation)
which tries to grow the quorum queue replica membership to the value of the key configured using `x-quorum-target-group-size`.
Take a look at the [rabbitmq.conf](https://www.rabbitmq.com/docs/quorum-queues#rabbitmqconf) to see
the default values of times when it triggered and with which interval.
