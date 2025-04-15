# Federating exchanges
Here, we will talk about federating exchanges. Exchange federation allows to replicate messages in exchanges
in one cluster to the other. In the [federation readme](https://github.com/blendereru/rabbitmq-dotnet-samples/tree/main/Plugins/Federation#examples),
we've discussed that they work through an internal queue:
![img.png](federation_exchange.png)

Here, `upstream queue` is the internal queue that we are talking about. The possession of this queue
allows resuming messages after the connection is lost.

## Example
In the directory, I provide a compose file, which has 2 nodes not connected with each other. We will
use them to show how the federation works in practice. There are one client per node, `cluster1.producer`
is `green` node's client, only publishes the message to the queue, and `cluster2.consumer` consumes the
messages coming from the queue that is bound to `federated.logs` exchange. Inside the `containers`,
execute the following commands:
1) enable the federation plugin, along with management(on both nodes)
```bash
rabbitmq-plugins enable rabbitmq_federation rabbitmq_federation_management
```
2) execute on green - set parameter on the `upstream` uri to `blue` node, with expiration(1 hour). 
```bash
rabbitmqctl set_parameter federation-upstream my-upstream '{"uri":"amqp://guest:guest@blue:5672", "expires":3600000}'
```
Then add the policy:
```bash
rabbitmqctl set_policy --apply-to exchanges federate-logs "^federated\.logs$" '{"federation-upstream":"my-upstream"}'
```
The policy applies to exchanges, and the policy matches `federated.logs` exchange exactly. We match with the upstream `my-upstream`,
instead of using `all` set of upstreams.

The example uses `fanout` exchange, so the routing key value doesn't play any role in this case. So, if 
we launch, `blue` node's consumer, we will successfully receive the message.

But how does configuration change when we try to receive messages from the upstream that is `direct` ?
Messages are published from `upstream` to `downstream` if and only if they match routing keys used in bindings
on the downstream exchange. The internal queue detects the downstream exchange bindings. If it has
queues bound with `info` and `warning`(example keys) the upstream exchange publish the messages
with this routing keys only to the upstream queue. Then `green` node pulls messages from the
upstream queue thus only receiving necessary messages.
> TL;DR - Federation uses only the bindings on the downstream exchange to decide what routing keys to mirror from the upstream exchange. Therefore, messages that match routing keys declared on the downstream exchange will be published into the downstream system — regardless of whether upstream had any queues for those keys.

That is why the configuration of parameter and policies doesn't change.

## Max hops
Federation exchanges have the `max-hops` limit. The term `max-hops` is needed to prevent messages being continually
copied and re-routed(endless cycle).Each time a message passes through a federation link, its hop count increases by 1.
If 2 clusters are mutually federated (A ↔ B), the same message can ping-pong back and forth endlessly.
Once a message exceeds `max-hops`, federated links will drop the message instead of forwarding it again.

Example configuration for max-hops:
```bash
rabbitmqctl set_parameter federation-upstream my-upstream '{"uri":"amqp://upstream-host", "max-hops": 2}'
```
This says: “Pull messages from upstream, but don’t pull ones that have already been federated more than 2 times.”




