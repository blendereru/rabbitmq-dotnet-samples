## Federated queues
Federated queues feature provides a way of load balancing(not message `replication`) the load of a single
local queue across different nodes or clusters. A downstream queue receives messages from an upstream 
queue `only` when the following conditions are met:
1) The downstream queue has active consumers requesting messages.
2) The downstream queue has no messages left to serve its consumers.
3) The upstream queue has unconsumed messages available.

If any of these conditions are not met for instance, if the `downstream` queue has no active consumers
the messages aren't pulled from the `upstream` queue.
Consider an example:
* Cluster A (Upstream): Hosts the original queue.
* Cluster B (Downstream): Hosts the federated queue linked to Cluster A's queue

If Cluster A has no active consumers for its queue, and Cluster B has consumers ready, the federated queue
in Cluster B will start pulling messages from Cluster A. Consider the following [image](https://www.rabbitmq.com/docs/federated-queues#overview) to understand the
concept:
![img.png](federated_queues.png)

Here, only the queues `log.info` and `log.warn` are federated, and usually only a
subset of queues in a cluster is federated.

## Features
1) Queue federation will not propagate `bindings` from the downstream to the upstreams. Let's understand
what that means through an example. Consider two RabbitMQ clusters(GPT generated):
* Cluster A (`Upstream`): Hosts an exchange named `logs` and a queue named `log_queue`.
* Cluster B (`Downstream`): Hosts a federated queue named `fed_log_queue` that connects to `log_queue` on Cluster A.
  
The `log_queue` on Cluster A is bound to the `logs` exchange with the routing key "error". On Cluster B,
`fed_log_queue` is set up to federate from `log_queue` on Cluster A. If you create a binding on
`fed_log_queue` to a local exchange on Cluster B with a different routing `key` (e.g., "warning"),
this binding does not influence `log_queue` on Cluster A. Messages published to the `logs` exchange on
Cluster A with the routing key "warning" will not reach `log_queue` on Cluster A
(since there's no binding for "warning"), and consequently, `fed_log_queue` on Cluster B will not
receive these messages, despite its local binding.

2) A federated queue can be "upstream" from another federated queue. One can even form "loops",
for example, queue A declares queue B to be upstream from it, and queue B declares queue A to be
upstream from it. More complex multiple-connected arrangements are allowed. But `max-hops` type of 
loops(for federated exchanges) handling is not implemented.

## Setup
Federating the queues is similar to exchange federation, except for when setting a `policy` we have to
apply it to `queues`. 
1) Set a parameter:
```bash
rabbitmqctl set_parameter federation-upstream blue '{"uri":"amqp://guest:guest@blue:5672"}'
```
Here, we set an upstream named `blue` from where we expect to pull messages.

2) Set a policy:
```bash
rabbitmqctl set_policy --vhost "/" --apply-to queues federate-orders "^federated\.orders$" '{"federation-upstream":"blue"}'
```
Here, we create a policy called `federate-orders` in the `default` virtual host. We try to match queues that start with
`federated.orders` and find an `exact` match of it in the `blue` upstream. So the `downstream` queue `federated.orders` will
pull messages from the `upstream` queue `federated.orders` in the `blue` node.

In the `cluster1.producer`, we publish a message to the upstream, but the consuming code is commented.
This is made for behaviour checking purposes. If the upstream queue has an active consumer, the downstream
federated queue will not receive any messages. And in the `cluster2.consumer` we only consume the messages
from the downstream queue. As soon as downstream queue receive one message that is not consumed, the messages
aren't pulled. So, its always important to keep in mind the 3 rules above.

