## Streams
[Streams](https://www.rabbitmq.com/docs/streams) in RabbitMQ are the queues that store and consume the messages in a different way.
From the documentation:
```text
Streams model an append-only log of messages that can be repeatedly read until they expire. Streams are always persistent and
replicated. To read messages from a stream in RabbitMQ, one or more consumers subscribe to it and read the same messages as many
times as they want. 
```
I can't actually imagine the real case when streams could be used instead of classic queues, but its better to know about its
existence. The use-cases for streams can be read [here](https://www.rabbitmq.com/docs/streams#use-cases)

## How to declare producer and consumer for Streams
We will use stream protocol