## Federation plugin
Federation is a mechanism that allows to `replicate` the messages across clusters. So 2 clusters don't need
to be connected in order to replicate some messages. In this approach, a federation plugin guarantees a
`loose` coupling of clusters.

## How does it work ?
When working with federation plugin there are some terms that we need to be familiar with:
* `upstream` - exchanges or queues that exist in remote clusters
* `federation link` - a connection to the upstream broker. In this case the downstream broker acts as a
AMQP client establishing a connection to the upstream broker. This connection is used to fetch messages
from the upstream.

## How to define federation ?
Federation definition is done in 2 ways:
1) Defining an upstream through a `parameter`. Can be set via `management UI` or through `CLI`.
2) Defining a policy that will match exchanges or queues. The [policy](https://www.rabbitmq.com/docs/federation#how-is-it-configured) will make the matched objects
(e.g. exchanges) federated, and one federation link (connection to other nodes) will be started for every match

## How to enable federation
In order to enable the federation, we have to add the `rabbitmq_federation` plugin, and optionally 
`rabbitmq_federation_management` to monitor federations, and enable policies directly from 
`management UI`:
```bash
rabbitmq-plugins enable rabbitmq_federation rabbitmq_federation_management
```
In order to enable federation in cluster, all nodes in the cluster must have federation plugin enabled.

## Examples
```bash
rabbitmqctl set_parameter federation-upstream my-upstream '{"uri":"amqp://target.hostname","expires":3600000}'
```
Defines an `upstream` with the `target.hostname` with `expiration` parameter being an hour. This parameter
is defined to the `internal upstream queue` that `buffers`(temporarily stores messages from the upstream
exchange before they are forwarded to the downstream broker) the messages from the upstream exchange(yes
this works for federation exchanges `only`).The internal queue binds to the upstream exchange using
bindings replicated from the downstream exchange, ensuring that only relevant messages are stored and
forwarded. So, the downstream starts receiving messages from that internal queue. The `expires` 
parameter deletes this queue after the duration set if the federation link is lost.

Then, we specify the policy:
```bash
rabbitmqctl set_policy --apply-to exchanges federate-me "^amq\." '{"federation-upstream-set":"all"}'
```
The configuration above creates a policy named `federate-me` and federates exchanges using the implicitly creates upstream set `all`. This set
instructs RabbitMQ to apply that policy to every available upstream without having to list them
individually.

The policy will try to match the exchanges of specified pattern with the ones(upstreams) specified in parameter.
In our example we don't specify any exchange in the upstream, so the policy will establish federation
links with upstream exchanges of the same name. If we have This means that a downstream exchange
named `amq.test` will attempt to federate with an upstream exchange also named `amq.test`.

If the upstream broker does not have an exchange named `amq.test`, the federation link for that exchange
will not be established.

However, if we set the parameter like this:
```bash
rabbitmqctl set_parameter federation-upstream my-upstream \
    '{"uri":"amqp://target.hostname","exchange":"amq.start","expires":3600000}'
```

the policy wWith this configuration, the downstream exchange `amq.test` (matched by the policy)
will federate with the upstream exchange `amq.start`.


