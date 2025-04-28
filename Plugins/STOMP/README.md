## STOMP plugin
STOMP is a protocol that is supported by RabbitMQ. It facilitates communication by defining a simple format
for messages, known as "frames," which are modeled after HTTP messages. A frame consists of the following
components:
1) `Command` - specifying the action to be performed.
2) `Headers` - Optional headers in the `key:value` format
3) `Body` - Goes after an empty line(which separates headers from the body), an optional message body.
4) `Terminator` -  A null character indicating the end of the frame.

## How it works
Initially, everything starts by initiating a handshake. First, a client sends `CONNECT` frame:
```txt
CONNECT
accept-version:1.0,1.1,2.0
host:stomp.github.org

^@
```
if the server shares the common version of protocol with the client responds with `CONNECTED` frame:
```text
CONNECTED
version:1.1

^@
```
Otherwise, responds with `ERROR` frame:
```text
ERROR
version:1.2,2.1
content-type:text/plain

Supported protocol versions are 1.2 2.1^@
```

After setting the connection(if successful), the client and server can interact with each other using various
frames:
1) `SEND` - sends a message to a destination. It MUST specify the `destination` header which can be one of 
the [following](https://www.rabbitmq.com/docs/stomp#d) types
2) `SUBSCRIBE` - registers a listener to the given destination. Also requires the `destination` header, from
which client wants to receive messages. After a message received on a subscribed destination, a `MESSAGE`
frame is sent from the server. The `id` header is mandatory, as it is needed to when unsubscribing from
the destination.

## TBA



